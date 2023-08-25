const path = require('path');

module.exports = {
    entry: {        
        "BlogEngine.min": './src/index.ts',
    },
    output: {
        filename: '[name].js',
        path: path.resolve(__dirname, '..', 'wwwroot'),
        library: {
            name: 'BlogEngine',
            type: "window",
        },
        module: true, // Enable outputting ES6 modules.
        environment: {
            module: true, // The environment supports ES6 modules.
        },
    },
    experiments: {
        outputModule: true, // This enables the experimental support for outputting ES6 modules
    },
    devtool: 'source-map',
    mode: 'production',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
            {
                test: /\.css$/,
                use: ['style-loader', 'css-loader'],
            },
            {
                test: /\.(eot|woff(2)?|ttf|otf|svg)$/i,
                type: 'asset'
            },
        ]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
};