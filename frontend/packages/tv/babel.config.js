module.exports = {
  presets: ['module:@react-native/babel-preset'],
  plugins: [
    [
      'module-resolver',
      {
        root: ['./src'],
        extensions: [
          '.ios.js',
          '.android.js',
          '.js',
          '.ts',
          '.tsx',
          '.json',
        ],
        alias: {
          '@': './src',
          '@components': './src/components',
          '@screens': './src/screens',
          '@navigation': './src/navigation',
          '@hooks': './src/hooks',
          '@store': './src/store',
          '@utils': './src/utils',
          '@types': './src/types',
          '@assets': './src/assets',
          '@theme': './src/theme',
          '@api': './src/api',
        },
      },
    ],
  ],
};