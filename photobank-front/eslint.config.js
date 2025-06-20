import js from '@eslint/js';
import tseslint from '@typescript-eslint/eslint-plugin';
import parser from '@typescript-eslint/parser';
import react from 'eslint-plugin-react';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import importPlugin from 'eslint-plugin-import';
import prettier from 'eslint-config-prettier';

/** @type {import("eslint").Linter.FlatConfig[]} */
export default [
    js.configs.recommended,
    {
        files: ['**/*.{ts,tsx}'],
        ignores: ['dist', 'build'],
        languageOptions: {
            parser,
            parserOptions: {
                project: './tsconfig.json',
                sourceType: 'module',
                ecmaVersion: 'latest',
                ecmaFeatures: {jsx: true},
            },
        },
        plugins: {
            '@typescript-eslint': tseslint,
            react,
            'react-hooks': reactHooks,
            'react-refresh': reactRefresh,
            'jsx-a11y': jsxA11y,
            import: importPlugin,
        },
        rules: {
            // TypeScript
            '@typescript-eslint/no-unused-vars': ['warn', {argsIgnorePattern: '^_'}],
            '@typescript-eslint/explicit-function-return-type': 'off',
            '@typescript-eslint/no-explicit-any': 'warn',

            // React
            'react/react-in-jsx-scope': 'off',
            'react/prop-types': 'off',
            'react/jsx-boolean-value': ['warn', 'never'],
            'react/jsx-no-leaked-render': ['error', {validStrategies: ['ternary']}],

            // React Hooks
            'react-hooks/rules-of-hooks': 'error',
            'react-hooks/exhaustive-deps': 'warn',

            // Accessibility
            'jsx-a11y/anchor-is-valid': 'warn',

            // Import
            'import/order': [
                'warn',
                {
                    groups: [
                        'builtin',
                        'external',
                        'internal',
                        'parent',
                        'sibling',
                        'index',
                        'object',
                        'type',
                    ],
                    alphabetize: {order: 'asc', caseInsensitive: true},
                    'newlines-between': 'always',
                },
            ],

            // React Fast Refresh
            'react-refresh/only-export-components': ['warn', {allowConstantExport: true}],

            // General
            'no-console': ['warn', {allow: ['warn', 'error']}],
            'no-debugger': 'warn',

            // Prettier
            'prettier/prettier': 'warn',
        },
        settings: {
            react: {
                version: 'detect',
            },
            'import/resolver': {
                typescript: {},
            },
        },
    },
    prettier, // must be last to override conflicting style rules
];
