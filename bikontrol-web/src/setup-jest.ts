// Jest setup for Angular tests (jest-preset-angular sets up zone.js + TestBed).
import { setupZoneTestEnv } from 'jest-preset-angular/setup-env/zone';

setupZoneTestEnv();

// Provide basic globals if needed by some libraries.
declare const global: any;
if (typeof global.TextEncoder === 'undefined') {
  global.TextEncoder = require('util').TextEncoder;
  global.TextDecoder = require('util').TextDecoder;
}
