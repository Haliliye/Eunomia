import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'playwright-report', 'test-results'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      // These four are React Compiler-readiness lints (aimed at code that
      // will run through the compiler) rather than bug detectors — they
      // flag conventional, working patterns used throughout this codebase
      // (setLoading(true) at the top of a data-fetching effect, calling
      // Date.now() in render for an "is this overdue" comparison, etc.).
      // Downgraded to warnings so `lint` surfaces them without blocking on
      // patterns that aren't actually broken.
      'react-hooks/set-state-in-effect': 'warn',
      'react-hooks/static-components': 'warn',
      'react-hooks/refs': 'warn',
      'react-hooks/purity': 'warn',
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      // The codebase leans on `any` deliberately in a handful of spots
      // (mostly axios error narrowing: `catch (err: any)`) — downgraded to a
      // warning rather than an error so lint is actionable instead of
      // blocking on a pattern used intentionally throughout.
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
    },
  },
)
