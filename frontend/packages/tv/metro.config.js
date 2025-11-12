const { getDefaultConfig, mergeConfig } = require('@react-native/metro-config');
const path = require('path');

/**
 * Metro configuration ��� TV � ���������� pnpm
 * @type {import('metro-config').MetroConfig}
 */
const config = {
  projectRoot: __dirname,
  
  // ��������� pnpm symlinks � workspace node_modules
  watchFolders: [
    __dirname,
    path.resolve(__dirname, '../../node_modules'),
  ],
  
  resolver: {
    // TV-����������� ���������� ������
    // Metro ����� ������ .tv.tsx, .android.tv.tsx, .ios.tv.tsx
    platforms: ['android', 'ios', 'native', 'tv'],
    
    sourceExts: [
      'tv.tsx',
      'tv.ts',
      'tv.jsx',
      'tv.js',
      'tsx',
      'ts',
      'jsx',
      'js',
      'json',
    ],
    
    // ��������� symlinks ��� pnpm
    unstable_enableSymlinks: true,
    unstable_enablePackageExports: true,

    // ������� ���� ��� ������ node_modules � pnpm workspace
    nodeModulesPaths: [
      path.resolve(__dirname, '../../node_modules'),
      path.resolve(__dirname, 'node_modules'),
    ],

    // ������� ������� � �������������� ������� React
    // ��������� �� ������ node_modules ������, ��� ��� react/react-native - ������ �����������
    extraNodeModules: {
      'react': path.resolve(__dirname, '../../node_modules/react'),
      'react-native': path.resolve(__dirname, '../../node_modules/react-native'),
    },
  },

  transformer: {
    getTransformOptions: async () => ({
      transform: {
        experimentalImportSupport: false,
        inlineRequires: true,
      },
    }),
  },
};

module.exports = mergeConfig(getDefaultConfig(__dirname), config);