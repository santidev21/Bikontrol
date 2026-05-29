/** Jest configuration for Bikontrol (Angular 18) */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/src/setup-jest.ts'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@env/environment$': '<rootDir>/src/environments/environment.ts',
    '\\.(scss|css|svg|png|jpg)$': '<rootDir>/__mocks__/styleMock.js'
  },
  transform: {
    '^.+\\.(ts|tsx|js|jsx|mjs)$': ['ts-jest', {
      tsconfig: 'tsconfig.spec.json',
      diagnostics: false
    }]
  },
  transformIgnorePatterns: ['/node_modules/(?!@angular|rxjs)'],
  testMatch: ['**/+(*.)+(spec|test).+(ts|js)?(x)'],
  moduleFileExtensions: ['ts','mjs','js','json','html']
};
