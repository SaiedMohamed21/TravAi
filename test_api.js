const http = require('http');

async function testApi() {
    console.log("Starting test...");
    try {
        const loginRes = await fetch('http://localhost:5210/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: 'user1@gmail.com', password: 'Password123!' })
        });
        const loginData = await loginRes.json();
        const token = loginData.token || loginData.Token;
        if (!token) {
            console.error("Login failed", loginData);
            return;
        }

        console.log("Logged in successfully!");

        // Booking ID from our query: 89, 90, 91
        const bookingId = 89;
        
        console.log(`\nFetching alternatives for Booking ID ${bookingId}...`);
        const altRes = await fetch(`http://localhost:5210/api/users/tour-cancellations/${bookingId}/alternatives`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const altData = await altRes.json();
        
        console.log("Alternatives returned:", altData.length);
        console.log(JSON.stringify(altData, null, 2));

    } catch (err) {
        console.error("Error:", err);
    }
}

testApi();
