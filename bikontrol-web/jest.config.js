/** Jest configuration for Bikontrol (Angular 22, via jest-preset-angular) */
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/src/setup-jest.ts'],
  moduleNameMapper: {
    '^@env/environment$': '<rootDir>/src/environments/environment.ts',
    '^@/(.*)$': '<rootDir>/src/$1',
    '\\.(scss|css|svg|png|jpg)$': '<rootDir>/__mocks__/styleMock.js'
  },
  testMatch: ['**/+(*.)+(spec|test).+(ts|js)?(x)']
};
