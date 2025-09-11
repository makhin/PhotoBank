// eslint.config.mjs
import tseslint from 'typescript-eslint';
import reactPlugin from 'eslint-plugin-react';
import reactHooks from 'eslint-plugin-react-hooks';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import importPlugin from 'eslint-plugin-import';
import unusedImports from 'eslint-plugin-unused-imports';
import prettier from 'eslint-config-prettier';

export default [
  {
    ignores: [
      '**/node_modules/**',
      'dist',
      'build',
      'coverage',
      'public',
      'out',
      'out-tsc',
      'lib',
      'cjs',
      'esm',
      'eslint.config.mjs',
      '**/*.test.ts',
      '**/*.test.tsx',
      '**/*.spec.ts',
      '**/*.spec.tsx',
      '**/*.msw.ts',
      '**/src/mocks/**',
      // сгенерированный orval код; при желании убери эту строку
      '**/api/photobank/**',
    ],
  },

  // Базовые правила TypeScript с type-aware проверками
  ...tseslint.configs.recommendedTypeChecked,

  {
    files: ['**/*.{ts,tsx}'],

    plugins: {
      react: reactPlugin,
      'react-hooks': reactHooks,
      'jsx-a11y': jsxA11y,
      import: importPlugin,
      'unused-imports': unusedImports,
    },

    languageOptions: {
      parserOptions: {
        // укажи монорепо-проекты; eslint запустит type-aware анализ
        project: ['./packages/**/tsconfig*.json'],
        tsconfigRootDir: import.meta.dirname,
        ecmaFeatures: { jsx: true },
      },
    },

    settings: {
      react: { version: 'detect' },
      'import/resolver': {
        // корректный резолв алиасов из tsconfig (монорепо)
        typescript: {
          project: ['./packages/**/tsconfig*.json'],
          alwaysTryTypes: true,
        },
        // и обычный node-резолвер, чтобы правило import/extensions работало предсказуемо
        node: {
          extensions: ['.ts', '.tsx', '.js', '.jsx', '.mts', '.cts'],
        },
      },
    },

    rules: {
      // React / Hooks / a11y
      ...reactPlugin.configs.recommended.rules,
      ...reactPlugin.configs['jsx-runtime'].rules,
      ...reactHooks.configs.recommended.rules,
      ...jsxA11y.configs.recommended.rules,

      // Порядок импортов
      'import/order': [
        'error',
        {
          groups: [
            ['builtin', 'external'],
            ['internal'],
            ['parent', 'sibling', 'index'],
          ],
          'newlines-between': 'always',
        },
      ],

      // Без неиспользуемых импортов
      'unused-imports/no-unused-imports': 'error',

      // TS/JS общие
      'react/react-in-jsx-scope': 'off',
      'react/prop-types': 'off',
      'no-undef': 'off',

      // Чуть ослабим строгие правила под твой кодстайл
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      '@typescript-eslint/explicit-module-boundary-types': 'off',
      '@typescript-eslint/no-floating-promises': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-call': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
      '@typescript-eslint/no-unsafe-return': 'off',
      '@typescript-eslint/no-redundant-type-constituents': 'off',
      '@typescript-eslint/no-invalid-void-type': 'off',

      // ГЛАВНОЕ: импорт/экспорт ТОЛЬКО без расширений
      'import/extensions': [
        'error',
        'ignorePackages',
        {
          js: 'never',
          jsx: 'never',
          ts: 'never',
          tsx: 'never',
          mjs: 'never',
          cjs: 'never',
        },
      ],

      // Избавляемся от ./index и лишних сегментов пути
      'import/no-useless-path-segments': ['error', { noUselessIndex: true }],
    },
  },

  // Совместимость с Prettier
  prettier,
];
