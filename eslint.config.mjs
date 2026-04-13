import js from '@eslint/js';
import globals from 'globals';

export default [
  {
    ignores: [
      '**/bin/**',
      '**/obj/**',
      '**/artifacts/**',
      '**/node_modules/**',
      '**/coverage/**',
      '**/dist/**',
      '**/.git/**',
      '**/.vs/**',
      '**/.vscode/**',
      '**/package-lock.json'
    ]
  },
  {
    files: ['**/*.{js,cjs,mjs}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.node
      }
    },
    rules: {
      ...js.configs.recommended.rules,
      'array-callback-return': 'error',
      'consistent-return': 'error',
      curly: ['error', 'all'],
      'dot-notation': 'error',
      eqeqeq: ['error', 'always'],
      'no-console': 'off',
      'no-constant-binary-expression': 'error',
      'no-duplicate-imports': 'error',
      'no-else-return': ['error', { allowElseIf: false }],
      'no-implicit-coercion': 'error',
      'no-shadow': 'error',
      'no-unneeded-ternary': 'error',
      'no-unused-vars': [
        'error',
        {
          args: 'after-used',
          argsIgnorePattern: '^_',
          caughtErrors: 'all',
          caughtErrorsIgnorePattern: '^_',
          varsIgnorePattern: '^_'
        }
      ],
      'no-useless-return': 'error',
      'object-shorthand': ['error', 'always'],
      'prefer-arrow-callback': 'error',
      'prefer-const': 'error',
      'prefer-template': 'error'
    }
  }
];
