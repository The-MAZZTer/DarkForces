const path = require('path');

module.exports = {
  mode: "development", // "production" | "development" | "none"
  entry: "./levelPreview.ts", // string | object | array
  output: {
    path: path.resolve(__dirname, "../Assets/WebGLTemplates/LevelPreview"), // string (default)
    filename: "levelPreview.js", // string (default)
    library: {
      type: "umd"
    }
  },
	module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: [{
          loader: "ts-loader"
        }],
        exclude: /node_modules/,
      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
  },
	devtool: 'source-map'
}
