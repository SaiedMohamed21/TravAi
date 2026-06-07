/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx,ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        display: ['Playfair Display', 'Georgia', 'serif'],
        body: ['Plus Jakarta Sans', 'sans-serif'],
        arabic: ['Noto Naskh Arabic', 'serif'],
      },
      colors: {
        sand: {
          50: '#fdfaf5',
          100: '#f9f1e4',
          200: '#f0ddb9',
          300: '#e5c98e',
          400: '#d4a853',
          500: '#c4922e',
          600: '#a67520',
          700: '#855c18',
          800: '#644514',
          900: '#43300f',
        },
        nile: {
          50: '#eef6ff',
          100: '#d9ecff',
          200: '#bcdcff',
          300: '#8ec5fd',
          400: '#59a3fa',
          500: '#3481f5',
          600: '#1e61e8',
          700: '#174dcf',
          800: '#1940a7',
          900: '#1a3b83',
        },
        pharaoh: {
          50: '#fdf4ff',
          100: '#fae8ff',
          200: '#f3d0fe',
          300: '#e9a8fd',
          400: '#d972fa',
          500: '#c246f0',
          600: '#a72bd3',
          700: '#8a21ad',
          800: '#721d8c',
          900: '#5e1b72',
        },
      },
      animation: {
        'fade-up': 'fadeUp 0.5s ease-out forwards',
        'pulse-slow': 'pulse 3s ease-in-out infinite',
        'shimmer': 'shimmer 1.5s infinite',
        'float': 'float 6s ease-in-out infinite',
      },
      keyframes: {
        fadeUp: {
          '0%': { opacity: '0', transform: 'translateY(16px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        shimmer: {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' },
        },
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-8px)' },
        },
      },
    },
  },
  plugins: [],
}
