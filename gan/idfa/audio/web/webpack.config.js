const path = require("path");
const webpack = require('webpack');

module.exports = {
  entry: {
        "app": "./index.js"
  },
  output: {
    path: path.resolve(__dirname, "./dist"),
    filename: "[name].bundle.js",
  },
  mode: "development",
  devServer: {
      contentBase: path.join(__dirname, "dist"),
      compress: true,
      hot: true
  },
  plugins: [
      new webpack.HotModuleReplacementPlugin()
  ],
  module: {
      rules: [
        {
          test: /\.js$/,
          exclude: /(node_modules|bower_components|wasm)/,
          use: {
            loader: 'babel-loader',
            options: {
              presets: ['@babel/env'],
              plugins: ["syntax-dynamic-import"]
            }
          }
        }
      ]
  }
};
