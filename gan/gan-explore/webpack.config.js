const path = require("path");
const webpack = require('webpack');
const ReactRefreshWebpackPlugin = require('@pmmmwh/react-refresh-webpack-plugin');

const isDevelopment = process.env.NODE_ENV !== 'production';

module.exports = {
  mode: isDevelopment ? 'development' : 'production',
  devServer: {
    contentBase: './static',
    host: '0.0.0.0',
    hot: true,
    disableHostCheck: true,
    historyApiFallback: true,
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, PATCH, OPTIONS",
      "Access-Control-Allow-Headers": "X-Requested-With, content-type, Authorization"
    }
  },
  devtool: 'source-map',
  entry: {
    app: ["./react-client/src/index.js"]
  },
  output: {
    path: path.resolve(__dirname, "static"),
    filename: "bundle.js"
  },
 plugins: [
     isDevelopment && new ReactRefreshWebpackPlugin()
  ].filter(Boolean),
  module: {
      rules: [
        {
          test: /\.js$/,
          exclude: /(node_modules)/,
          use: {
            loader: 'babel-loader',
            options: {
              plugins: [isDevelopment && require.resolve('react-refresh/babel')].filter(Boolean),
              presets: [['@babel/preset-env',{useBuiltIns: 'usage', corejs: 3}], '@babel/preset-react']
            }
          }
        },
        {
         test: /\.css$/,
         use: ['style-loader', 'css-loader']
        },
        {
         test: /\.less$/,
         use: ['less-loader']
        },
        {
         test: /\.(png|svg|jpg|gif)$/,
         use: [
           'file-loader',
         ],
        },
      {
        test: /\.s[ac]ss$/i,
        use: [
          // Creates `style` nodes from JS strings
          'style-loader',
          // Translates CSS into CommonJS
          'css-loader',
          // Compiles Sass to CSS
          'sass-loader',
        ],
      },
     ]
  }
};
