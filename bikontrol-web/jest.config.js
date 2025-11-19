/** Jest configuration for Bikontrol (Angular 18) */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/src/setup-jest.ts'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '\\.(scss|css|svg|png|jpg)$': '<rootDir>/__mocks__/styleMock.js'
  },
  transform: {
    '^.+\\.(ts|tsx|js|jsx|mjs)$': 'ts-jest'
  },
  transformIgnorePatterns: ['/node_modules/(?!@angular|rxjs)'],
  globals: {
    'ts-jest': {
      tsconfig: 'tsconfig.spec.json',
      diagnostics: false
    }
  },
  testMatch: ['**/+(*.)+(spec|test).+(ts|js)?(x)'],
  moduleFileExtensions: ['ts','mjs','js','json','html']
};
