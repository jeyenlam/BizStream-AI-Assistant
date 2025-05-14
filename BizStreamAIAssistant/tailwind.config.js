///** @type {import('tailwindcss').Config} */
//module.exports = {
//    content: [
//        './**/*.html', // Add all paths where your HTML or Razor files exist
//        './**/*.razor',
//        './**/*.cshtml',
//    ],
//    theme: {
//        extend: {},
//    },
//    plugins: [],
//}


module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Pages/**/*.cshtml',  // if using Razor Pages too
    ],
    theme: {
        extend: {},
    },
    plugins: [],
};
