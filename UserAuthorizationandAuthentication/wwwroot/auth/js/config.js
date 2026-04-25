let API_BASE_URL = '/api';
let BASE_URL = '';

const Auth = {
    getToken: () => localStorage.getItem('token'),
    setToken: (token) => localStorage.setItem('token', token),
    clearToken: () => localStorage.removeItem('token'),
    isLoggedIn: () => !!localStorage.getItem('token'),
    getUser: () => {
        try {
            return JSON.parse(localStorage.getItem('userValues') || '{}');
        } catch {
            return {};
        }
    },
    logout: () => {
        localStorage.removeItem('token');
        localStorage.removeItem('userValues');
        window.location.href = '/auth/login.html';
    }
};

const API = {
    request: async function (url, options = {}) {
        const token = Auth.getToken();
        const headers = {
            ...(token && { 'Authorization': `Bearer ${token}` })
        };
        
        // Only set JSON content type if not FormData
        if (!(options.body instanceof FormData)) {
            headers['Content-Type'] = 'application/json';
        }

        try {
            const fetchOptions = {
                ...options,
                headers: { ...headers, ...options.headers }
            };
            
            // Auto-stringify if not FormData or string
            if (options.body && !(options.body instanceof FormData) && typeof options.body !== 'string') {
                fetchOptions.body = JSON.stringify(options.body);
            }

            const response = await fetch(`${API_BASE_URL}${url}`, fetchOptions);

            // Handle 401 Unauthorized
            if (response.status === 401) {
                Auth.logout();
                return { success: false, message: 'Session expired', data: null };
            }

            // Parse JSON response
            const jsonData = await response.json();

            if (!response.ok) {
                return {
                    success: false,
                    message: jsonData.message || jsonData.Message || 'Request failed',
                    data: null,
                    errors: jsonData.errors || jsonData.Errors || []
                };
            }

            // Backend returns ApiResponse<T> with structure: { success, message, data, errors }
            // Ensure res.data is ONLY the payload, or null if empty.
            let extractedData = null;
            if (jsonData.hasOwnProperty('data')) extractedData = jsonData.data;
            else if (jsonData.hasOwnProperty('Data')) extractedData = jsonData.Data;
            else extractedData = jsonData; // Fallback for unwrapped responses

            return {
                success: jsonData.success !== undefined ? jsonData.success : true,
                message: jsonData.message || jsonData.Message || 'Success',
                data: extractedData,
                errors: jsonData.errors || jsonData.Errors || []
            };
        } catch (error) {
            console.error('API Error:', error);
            return {
                success: false,
                message: 'Network error occurred',
                data: null,
                errors: [error.message]
            };
        }
    },

    get: function (url) {
        return this.request(url, { method: 'GET' });
    },

    post: function (url, data) {
        return this.request(url, { method: 'POST', body: data });
    },

    put: function (url, data) {
        return this.request(url, { method: 'PUT', body: data });
    },

    delete: function (url) {
        return this.request(url, { method: 'DELETE' });
    }
};
