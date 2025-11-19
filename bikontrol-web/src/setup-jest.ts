// Minimal Jest setup for Angular services tests.
// We intentionally do not use `jest-preset-angular` because it
// currently requires versions incompatible with Angular 18.

import 'zone.js';
import 'zone.js/testing';
import { TestBed } from '@angular/core/testing';
import { BrowserDynamicTestingModule, platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing';

TestBed.initTestEnvironment(BrowserDynamicTestingModule, platformBrowserDynamicTesting());

// Provide basic globals if needed by some libraries.
declare const global: any;
if (typeof global.TextEncoder === 'undefined') {
	global.TextEncoder = require('util').TextEncoder;
	global.TextDecoder = require('util').TextDecoder;
}

