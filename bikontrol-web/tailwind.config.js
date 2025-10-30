/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}"
  ],
  theme: {
    extend: {
      colors: {
        bg: '#f8f8f8',
        card: '#b4d2e7',
        primary: '#003559',
        secondary: '#94c5cc',
        accent: '#a1a6b4',
        textdark: '#003559',
        textlight: '#ffffff',
      },
    },
  },
  plugins: [],
};
