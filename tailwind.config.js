/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Views/**/*.{cshtml,html}",
        "./Pages/**/*.{cshtml,html}",
        "./Components/**/*.{cshtml,html}",
    ],
    theme: {
        extend: {
            colors: {
                'kseb-blue': '#0056b3',
                'kseb-green': '#28a745',
                'kseb-yellow': '#ffc107',
                'kseb-red': '#dc3545',
            },
        },
    },
    plugins: [],
}