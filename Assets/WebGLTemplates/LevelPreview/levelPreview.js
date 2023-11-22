(function webpackUniversalModuleDefinition(root, factory) {
	if(typeof exports === 'object' && typeof module === 'object')
		module.exports = factory();
	else if(typeof define === 'function' && define.amd)
		define([], factory);
	else {
		var a = factory();
		for(var i in a) (typeof exports === 'object' ? exports : root)[i] = a[i];
	}
})(self, () => {
return /******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./node_modules/tslib/tslib.es6.mjs":
/*!******************************************!*\
  !*** ./node_modules/tslib/tslib.es6.mjs ***!
  \******************************************/
/***/ ((__unused_webpack___webpack_module__, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   __addDisposableResource: () => (/* binding */ __addDisposableResource),
/* harmony export */   __assign: () => (/* binding */ __assign),
/* harmony export */   __asyncDelegator: () => (/* binding */ __asyncDelegator),
/* harmony export */   __asyncGenerator: () => (/* binding */ __asyncGenerator),
/* harmony export */   __asyncValues: () => (/* binding */ __asyncValues),
/* harmony export */   __await: () => (/* binding */ __await),
/* harmony export */   __awaiter: () => (/* binding */ __awaiter),
/* harmony export */   __classPrivateFieldGet: () => (/* binding */ __classPrivateFieldGet),
/* harmony export */   __classPrivateFieldIn: () => (/* binding */ __classPrivateFieldIn),
/* harmony export */   __classPrivateFieldSet: () => (/* binding */ __classPrivateFieldSet),
/* harmony export */   __createBinding: () => (/* binding */ __createBinding),
/* harmony export */   __decorate: () => (/* binding */ __decorate),
/* harmony export */   __disposeResources: () => (/* binding */ __disposeResources),
/* harmony export */   __esDecorate: () => (/* binding */ __esDecorate),
/* harmony export */   __exportStar: () => (/* binding */ __exportStar),
/* harmony export */   __extends: () => (/* binding */ __extends),
/* harmony export */   __generator: () => (/* binding */ __generator),
/* harmony export */   __importDefault: () => (/* binding */ __importDefault),
/* harmony export */   __importStar: () => (/* binding */ __importStar),
/* harmony export */   __makeTemplateObject: () => (/* binding */ __makeTemplateObject),
/* harmony export */   __metadata: () => (/* binding */ __metadata),
/* harmony export */   __param: () => (/* binding */ __param),
/* harmony export */   __propKey: () => (/* binding */ __propKey),
/* harmony export */   __read: () => (/* binding */ __read),
/* harmony export */   __rest: () => (/* binding */ __rest),
/* harmony export */   __runInitializers: () => (/* binding */ __runInitializers),
/* harmony export */   __setFunctionName: () => (/* binding */ __setFunctionName),
/* harmony export */   __spread: () => (/* binding */ __spread),
/* harmony export */   __spreadArray: () => (/* binding */ __spreadArray),
/* harmony export */   __spreadArrays: () => (/* binding */ __spreadArrays),
/* harmony export */   __values: () => (/* binding */ __values),
/* harmony export */   "default": () => (__WEBPACK_DEFAULT_EXPORT__)
/* harmony export */ });
/******************************************************************************
Copyright (c) Microsoft Corporation.

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.
***************************************************************************** */
/* global Reflect, Promise, SuppressedError, Symbol */

var extendStatics = function(d, b) {
  extendStatics = Object.setPrototypeOf ||
      ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
      function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
  return extendStatics(d, b);
};

function __extends(d, b) {
  if (typeof b !== "function" && b !== null)
      throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
  extendStatics(d, b);
  function __() { this.constructor = d; }
  d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
}

var __assign = function() {
  __assign = Object.assign || function __assign(t) {
      for (var s, i = 1, n = arguments.length; i < n; i++) {
          s = arguments[i];
          for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p)) t[p] = s[p];
      }
      return t;
  }
  return __assign.apply(this, arguments);
}

function __rest(s, e) {
  var t = {};
  for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
      t[p] = s[p];
  if (s != null && typeof Object.getOwnPropertySymbols === "function")
      for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
          if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
              t[p[i]] = s[p[i]];
      }
  return t;
}

function __decorate(decorators, target, key, desc) {
  var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
  if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
  else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
  return c > 3 && r && Object.defineProperty(target, key, r), r;
}

function __param(paramIndex, decorator) {
  return function (target, key) { decorator(target, key, paramIndex); }
}

function __esDecorate(ctor, descriptorIn, decorators, contextIn, initializers, extraInitializers) {
  function accept(f) { if (f !== void 0 && typeof f !== "function") throw new TypeError("Function expected"); return f; }
  var kind = contextIn.kind, key = kind === "getter" ? "get" : kind === "setter" ? "set" : "value";
  var target = !descriptorIn && ctor ? contextIn["static"] ? ctor : ctor.prototype : null;
  var descriptor = descriptorIn || (target ? Object.getOwnPropertyDescriptor(target, contextIn.name) : {});
  var _, done = false;
  for (var i = decorators.length - 1; i >= 0; i--) {
      var context = {};
      for (var p in contextIn) context[p] = p === "access" ? {} : contextIn[p];
      for (var p in contextIn.access) context.access[p] = contextIn.access[p];
      context.addInitializer = function (f) { if (done) throw new TypeError("Cannot add initializers after decoration has completed"); extraInitializers.push(accept(f || null)); };
      var result = (0, decorators[i])(kind === "accessor" ? { get: descriptor.get, set: descriptor.set } : descriptor[key], context);
      if (kind === "accessor") {
          if (result === void 0) continue;
          if (result === null || typeof result !== "object") throw new TypeError("Object expected");
          if (_ = accept(result.get)) descriptor.get = _;
          if (_ = accept(result.set)) descriptor.set = _;
          if (_ = accept(result.init)) initializers.unshift(_);
      }
      else if (_ = accept(result)) {
          if (kind === "field") initializers.unshift(_);
          else descriptor[key] = _;
      }
  }
  if (target) Object.defineProperty(target, contextIn.name, descriptor);
  done = true;
};

function __runInitializers(thisArg, initializers, value) {
  var useValue = arguments.length > 2;
  for (var i = 0; i < initializers.length; i++) {
      value = useValue ? initializers[i].call(thisArg, value) : initializers[i].call(thisArg);
  }
  return useValue ? value : void 0;
};

function __propKey(x) {
  return typeof x === "symbol" ? x : "".concat(x);
};

function __setFunctionName(f, name, prefix) {
  if (typeof name === "symbol") name = name.description ? "[".concat(name.description, "]") : "";
  return Object.defineProperty(f, "name", { configurable: true, value: prefix ? "".concat(prefix, " ", name) : name });
};

function __metadata(metadataKey, metadataValue) {
  if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(metadataKey, metadataValue);
}

function __awaiter(thisArg, _arguments, P, generator) {
  function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
  return new (P || (P = Promise))(function (resolve, reject) {
      function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
      function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
      function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
      step((generator = generator.apply(thisArg, _arguments || [])).next());
  });
}

function __generator(thisArg, body) {
  var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
  return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
  function verb(n) { return function (v) { return step([n, v]); }; }
  function step(op) {
      if (f) throw new TypeError("Generator is already executing.");
      while (g && (g = 0, op[0] && (_ = 0)), _) try {
          if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
          if (y = 0, t) op = [op[0] & 2, t.value];
          switch (op[0]) {
              case 0: case 1: t = op; break;
              case 4: _.label++; return { value: op[1], done: false };
              case 5: _.label++; y = op[1]; op = [0]; continue;
              case 7: op = _.ops.pop(); _.trys.pop(); continue;
              default:
                  if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                  if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                  if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                  if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                  if (t[2]) _.ops.pop();
                  _.trys.pop(); continue;
          }
          op = body.call(thisArg, _);
      } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
      if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
  }
}

var __createBinding = Object.create ? (function(o, m, k, k2) {
  if (k2 === undefined) k2 = k;
  var desc = Object.getOwnPropertyDescriptor(m, k);
  if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
  }
  Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
  if (k2 === undefined) k2 = k;
  o[k2] = m[k];
});

function __exportStar(m, o) {
  for (var p in m) if (p !== "default" && !Object.prototype.hasOwnProperty.call(o, p)) __createBinding(o, m, p);
}

function __values(o) {
  var s = typeof Symbol === "function" && Symbol.iterator, m = s && o[s], i = 0;
  if (m) return m.call(o);
  if (o && typeof o.length === "number") return {
      next: function () {
          if (o && i >= o.length) o = void 0;
          return { value: o && o[i++], done: !o };
      }
  };
  throw new TypeError(s ? "Object is not iterable." : "Symbol.iterator is not defined.");
}

function __read(o, n) {
  var m = typeof Symbol === "function" && o[Symbol.iterator];
  if (!m) return o;
  var i = m.call(o), r, ar = [], e;
  try {
      while ((n === void 0 || n-- > 0) && !(r = i.next()).done) ar.push(r.value);
  }
  catch (error) { e = { error: error }; }
  finally {
      try {
          if (r && !r.done && (m = i["return"])) m.call(i);
      }
      finally { if (e) throw e.error; }
  }
  return ar;
}

/** @deprecated */
function __spread() {
  for (var ar = [], i = 0; i < arguments.length; i++)
      ar = ar.concat(__read(arguments[i]));
  return ar;
}

/** @deprecated */
function __spreadArrays() {
  for (var s = 0, i = 0, il = arguments.length; i < il; i++) s += arguments[i].length;
  for (var r = Array(s), k = 0, i = 0; i < il; i++)
      for (var a = arguments[i], j = 0, jl = a.length; j < jl; j++, k++)
          r[k] = a[j];
  return r;
}

function __spreadArray(to, from, pack) {
  if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
      if (ar || !(i in from)) {
          if (!ar) ar = Array.prototype.slice.call(from, 0, i);
          ar[i] = from[i];
      }
  }
  return to.concat(ar || Array.prototype.slice.call(from));
}

function __await(v) {
  return this instanceof __await ? (this.v = v, this) : new __await(v);
}

function __asyncGenerator(thisArg, _arguments, generator) {
  if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
  var g = generator.apply(thisArg, _arguments || []), i, q = [];
  return i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i;
  function verb(n) { if (g[n]) i[n] = function (v) { return new Promise(function (a, b) { q.push([n, v, a, b]) > 1 || resume(n, v); }); }; }
  function resume(n, v) { try { step(g[n](v)); } catch (e) { settle(q[0][3], e); } }
  function step(r) { r.value instanceof __await ? Promise.resolve(r.value.v).then(fulfill, reject) : settle(q[0][2], r); }
  function fulfill(value) { resume("next", value); }
  function reject(value) { resume("throw", value); }
  function settle(f, v) { if (f(v), q.shift(), q.length) resume(q[0][0], q[0][1]); }
}

function __asyncDelegator(o) {
  var i, p;
  return i = {}, verb("next"), verb("throw", function (e) { throw e; }), verb("return"), i[Symbol.iterator] = function () { return this; }, i;
  function verb(n, f) { i[n] = o[n] ? function (v) { return (p = !p) ? { value: __await(o[n](v)), done: false } : f ? f(v) : v; } : f; }
}

function __asyncValues(o) {
  if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
  var m = o[Symbol.asyncIterator], i;
  return m ? m.call(o) : (o = typeof __values === "function" ? __values(o) : o[Symbol.iterator](), i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i);
  function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }
  function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }
}

function __makeTemplateObject(cooked, raw) {
  if (Object.defineProperty) { Object.defineProperty(cooked, "raw", { value: raw }); } else { cooked.raw = raw; }
  return cooked;
};

var __setModuleDefault = Object.create ? (function(o, v) {
  Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
  o["default"] = v;
};

function __importStar(mod) {
  if (mod && mod.__esModule) return mod;
  var result = {};
  if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
  __setModuleDefault(result, mod);
  return result;
}

function __importDefault(mod) {
  return (mod && mod.__esModule) ? mod : { default: mod };
}

function __classPrivateFieldGet(receiver, state, kind, f) {
  if (kind === "a" && !f) throw new TypeError("Private accessor was defined without a getter");
  if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot read private member from an object whose class did not declare it");
  return kind === "m" ? f : kind === "a" ? f.call(receiver) : f ? f.value : state.get(receiver);
}

function __classPrivateFieldSet(receiver, state, value, kind, f) {
  if (kind === "m") throw new TypeError("Private method is not writable");
  if (kind === "a" && !f) throw new TypeError("Private accessor was defined without a setter");
  if (typeof state === "function" ? receiver !== state || !f : !state.has(receiver)) throw new TypeError("Cannot write private member to an object whose class did not declare it");
  return (kind === "a" ? f.call(receiver, value) : f ? f.value = value : state.set(receiver, value)), value;
}

function __classPrivateFieldIn(state, receiver) {
  if (receiver === null || (typeof receiver !== "object" && typeof receiver !== "function")) throw new TypeError("Cannot use 'in' operator on non-object");
  return typeof state === "function" ? receiver === state : state.has(receiver);
}

function __addDisposableResource(env, value, async) {
  if (value !== null && value !== void 0) {
    if (typeof value !== "object" && typeof value !== "function") throw new TypeError("Object expected.");
    var dispose;
    if (async) {
        if (!Symbol.asyncDispose) throw new TypeError("Symbol.asyncDispose is not defined.");
        dispose = value[Symbol.asyncDispose];
    }
    if (dispose === void 0) {
        if (!Symbol.dispose) throw new TypeError("Symbol.dispose is not defined.");
        dispose = value[Symbol.dispose];
    }
    if (typeof dispose !== "function") throw new TypeError("Object not disposable.");
    env.stack.push({ value: value, dispose: dispose, async: async });
  }
  else if (async) {
    env.stack.push({ async: true });
  }
  return value;
}

var _SuppressedError = typeof SuppressedError === "function" ? SuppressedError : function (error, suppressed, message) {
  var e = new Error(message);
  return e.name = "SuppressedError", e.error = error, e.suppressed = suppressed, e;
};

function __disposeResources(env) {
  function fail(e) {
    env.error = env.hasError ? new _SuppressedError(e, env.error, "An error was suppressed during disposal.") : e;
    env.hasError = true;
  }
  function next() {
    while (env.stack.length) {
      var rec = env.stack.pop();
      try {
        var result = rec.dispose && rec.dispose.call(rec.value);
        if (rec.async) return Promise.resolve(result).then(next, function(e) { fail(e); return next(); });
      }
      catch (e) {
          fail(e);
      }
    }
    if (env.hasError) throw env.error;
  }
  return next();
}

/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = ({
  __extends,
  __assign,
  __rest,
  __decorate,
  __param,
  __metadata,
  __awaiter,
  __generator,
  __createBinding,
  __exportStar,
  __values,
  __read,
  __spread,
  __spreadArrays,
  __spreadArray,
  __await,
  __asyncGenerator,
  __asyncDelegator,
  __asyncValues,
  __makeTemplateObject,
  __importStar,
  __importDefault,
  __classPrivateFieldGet,
  __classPrivateFieldSet,
  __classPrivateFieldIn,
  __addDisposableResource,
  __disposeResources,
});


/***/ })

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		var cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/define property getters */
/******/ 	(() => {
/******/ 		// define getter functions for harmony exports
/******/ 		__webpack_require__.d = (exports, definition) => {
/******/ 			for(var key in definition) {
/******/ 				if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 					Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	(() => {
/******/ 		__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop))
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	(() => {
/******/ 		// define __esModule on exports
/******/ 		__webpack_require__.r = (exports) => {
/******/ 			if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 				Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 			}
/******/ 			Object.defineProperty(exports, '__esModule', { value: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
var __webpack_exports__ = {};
// This entry need to be wrapped in an IIFE because it need to be isolated against other modules in the chunk.
(() => {
var exports = __webpack_exports__;
/*!*************************!*\
  !*** ./levelPreview.ts ***!
  \*************************/

Object.defineProperty(exports, "__esModule", ({ value: true }));
exports.LevelPreview = void 0;
const tslib_1 = __webpack_require__(/*! tslib */ "./node_modules/tslib/tslib.es6.mjs");
class LevelPreview {
    constructor() {
        this.lastId = 0;
        this.pendingCalls = {};
    }
    createUnityInstanceAsync(unityData, canvas) {
        return tslib_1.__awaiter(this, void 0, void 0, function* () {
            window.__levelPreview = this;
            return this.gameInstance = yield window.createUnityInstance(canvas, unityData);
        });
    }
    apiCall(api, ...args) {
        const id = ++this.lastId;
        const promise = new Promise(x => this.pendingCalls[id] = x);
        this.gameInstance.SendMessage("Level", "OnApiCall", JSON.stringify({ Id: id, Api: api, Args: args }));
        return promise;
    }
    __onApiCallFinished(id) {
        const callback = this.pendingCalls[id];
        delete this.pendingCalls[id];
        callback();
    }
    quit() {
        return this.apiCall("Quit");
    }
    captureMouse() {
        return this.apiCall("CaptureMouse");
    }
    releaseMouse() {
        return this.apiCall("ReleaseMouse");
    }
    reloadDataFiles() {
        return this.apiCall("ReloadDataFiles");
    }
    addModFile(path) {
        return this.apiCall("AddModFile", path);
    }
    loadLevelList() {
        return this.apiCall("LoadLevelList");
    }
    loadLevel(index) {
        return this.apiCall("LoadLevel", index.toString());
    }
    reloadLevelInPlace() {
        return this.apiCall("ReloadLevelInPlace");
    }
    initEmptyLevel(name, musicIndex, paletteName) {
        return this.apiCall("InitEmptyLevel", name, musicIndex.toString(), paletteName);
    }
    setDarkForcesPath(path) {
        return this.apiCall("SetDarkForcesPath", path);
    }
    setBackground(r, g, b) {
        return this.apiCall("SetBackground", r.toString(), g.toString(), b.toString());
    }
    setShowWaitBitmap(value) {
        return this.apiCall("SetShowWaitBitmap", value.toString());
    }
    setExtendSkyPit(value) {
        return this.apiCall("SetExtendSkyPit", value.toString());
    }
    setShowSprites(value) {
        return this.apiCall("SetShowSprites", value.toString());
    }
    setShow3dos(value) {
        return this.apiCall("SetShow3dos", value.toString());
    }
    setDifficulty(value) {
        return this.apiCall("SetDifficulty", value.toString());
    }
    setAnimateVues(value) {
        return this.apiCall("SetAnimateVues", value.toString());
    }
    setAnimate3doUpdates(value) {
        return this.apiCall("SetAnimate3doUpdates", value.toString());
    }
    setFullBrightLighting(value) {
        return this.apiCall("SetFullBrightLighting", value.toString());
    }
    setBypassColormapDithering(value) {
        return this.apiCall("SetBypassColormapDithering", value.toString());
    }
    setPlayMusic(value) {
        return this.apiCall("SetPlayMusic", value.toString());
    }
    setPlayFightTrack(value) {
        return this.apiCall("SetPlayFightTrack", value.toString());
    }
    setVolume(value) {
        return this.apiCall("SetVolume", value.toString());
    }
    setVisibleLayer(value) {
        if (value === undefined || value === null) {
            return this.apiCall("SetVisibleLayer");
        }
        else {
            return this.apiCall("SetVisibleLayer", value.toString());
        }
    }
    setLookSensitivity(x, y) {
        return this.apiCall("SetLookSensitivity", x.toString(), y.toString());
    }
    setInvertYLook(value) {
        return this.apiCall("SetInvertYLook", value.toString());
    }
    setMoveSensitivity(x, y, z) {
        return this.apiCall("SetMoveSensitivity", x.toString(), y.toString(), z.toString());
    }
    setYawLimits(min, max) {
        return this.apiCall("SetYawLimits", min.toString(), max.toString());
    }
    setRunMultiplier(value) {
        return this.apiCall("SetRunMultiplier", value.toString());
    }
    setZoomSensitivity(value) {
        return this.apiCall("SetZoomSensitivity", value.toString());
    }
    setUseOrbitCamera(value) {
        return this.apiCall("SetUseOrbitCamera", value.toString());
    }
    setUseMouseCapture(value) {
        return this.apiCall("SetUseMouseCapture", value.toString());
    }
    setShowHud(value) {
        return this.apiCall("SetShowHud", value.toString());
    }
    setHudAlign(value) {
        return this.apiCall("SetHudAlign", value.toString());
    }
    setHudFontSize(value) {
        return this.apiCall("SetHudFontSize", value.toString());
    }
    setHudColor(r, g, b, a) {
        return this.apiCall("SetHudColor", r.toString(), g.toString(), b.toString(), a.toString());
    }
    setShowHudCoordinates(value) {
        return this.apiCall("SetShowHudCoordinates", value.toString());
    }
    setHudFpsCoordinates(value) {
        return this.apiCall("SetHudFpsCoordinates", value);
    }
    setHudOrbitCoordinates(value) {
        return this.apiCall("SetHudOrbitCoordinates", value);
    }
    setShowHudRaycastHit(value) {
        return this.apiCall("SetShowHudRaycastHit", value.toString());
    }
    setHudRaycastFloor(value) {
        return this.apiCall("SetHudRaycastFloor", value);
    }
    setHudRaycastCeiling(value) {
        return this.apiCall("SetHudRaycastCeiling", value);
    }
    setHudRaycastWall(value) {
        return this.apiCall("SetHudRaycastWall", value);
    }
    setHudRaycastObject(value) {
        return this.apiCall("SetHudRaycastObject", value);
    }
    reloadLevelGeometry(level) {
        return this.apiCall("ReloadLevelGeometry", JSON.stringify(level));
    }
    setLevelMetadata(levelFile, musicFile, paletteFile, parallaxX, parallaxY) {
        return this.apiCall("SetLevelMetadata", levelFile, musicFile, paletteFile, parallaxX.toString(), parallaxY.toString());
    }
    reloadSector(index, sector) {
        return this.apiCall("ReloadSector", index.toString(), JSON.stringify(sector));
    }
    setSector(index, sector) {
        return this.apiCall("SetSector", index.toString(), JSON.stringify(sector));
    }
    moveSector(index, x, y, z) {
        return this.apiCall("MoveSector", index.toString(), x.toString(), y.toString(), z.toString());
    }
    deleteSector(index) {
        return this.apiCall("DeleteSector", index.toString());
    }
    setSectorFloor(index, floor) {
        return this.apiCall("SetSectorFloor", index.toString(), JSON.stringify(floor));
    }
    setSectorCeiling(index, ceiling) {
        return this.apiCall("SetSectorCeiling", index.toString(), JSON.stringify(ceiling));
    }
    reloadWall(sectorIndex, wallIndex, wall) {
        return this.apiCall("ReloadWall", sectorIndex.toString(), wallIndex.toString(), JSON.stringify(wall));
    }
    insertWall(sectorIndex, wallIndex, wall) {
        return this.apiCall("InsertWall", sectorIndex.toString(), wallIndex.toString(), JSON.stringify(wall));
    }
    deleteWall(sectorIndex, wallIndex) {
        return this.apiCall("DeleteWall", sectorIndex.toString(), wallIndex.toString());
    }
    setVertex(sectorIndex, wallIndex, rightVertex, x, z) {
        return this.apiCall("SetVertex", sectorIndex.toString(), wallIndex.toString(), rightVertex.toString(), x.toString(), z.toString());
    }
    reloadLevelObjects(objects) {
        return this.apiCall("ReloadLevelObjects", JSON.stringify(objects));
    }
    setObject(index, object) {
        return this.apiCall("SetObject", index.toString(), JSON.stringify(object));
    }
    deleteObject(index) {
        return this.apiCall("DeleteObject", index.toString());
    }
    resetCamera() {
        return this.apiCall("ResetCamera");
    }
    moveCamera(x, y, z) {
        return this.apiCall("MoveCamera", x.toString(), y.toString(), z.toString());
    }
    rotateCamera(w, x, y, z) {
        return this.apiCall("RotateCamera", w.toString(), x.toString(), y.toString(), z.toString());
    }
    rotateCameraEuler(pitch, yaw, roll) {
        return this.apiCall("RotateCameraEuler", pitch.toString(), yaw.toString(), roll.toString());
    }
    moveAndRotateCamera(posX, posY, posZ, rotW, rotX, rotY, rotZ) {
        return this.apiCall("MoveAndRotateCamera", posX.toString(), posY.toString(), posZ.toString(), rotW.toString(), rotX.toString(), rotY.toString(), rotZ.toString());
    }
    moveAndRotateCameraEuler(x, y, z, pitch, yaw, roll) {
        return this.apiCall("MoveAndRotateCameraEuler", x.toString(), y.toString(), z.toString(), pitch.toString(), yaw.toString(), roll.toString());
    }
    pointCameraAt(x, y, z) {
        return this.apiCall("PointCameraAt", x.toString(), y.toString(), z.toString());
    }
    __onEvent(eventName, ...args) {
        var _a, _b, _c, _d, _e, _f, _g, _h, _j;
        switch (eventName) {
            case "OnReady":
                (_a = this.onReady) === null || _a === void 0 ? void 0 : _a.call(this);
                break;
            case "OnLoadError":
                (_b = this.onLoadError) === null || _b === void 0 ? void 0 : _b.call(this, args[0], parseInt(args[1], 10), args[2]);
                break;
            case "OnLoadWarning":
                (_c = this.onLoadWarning) === null || _c === void 0 ? void 0 : _c.call(this, args[0], parseInt(args[1], 10), args[2]);
                break;
            case "OnLevelListLoaded":
                (_d = this.onLevelListLoaded) === null || _d === void 0 ? void 0 : _d.call(this, JSON.parse(args[0]));
                break;
            case "OnLevelLoaded":
                (_e = this.onLevelLoaded) === null || _e === void 0 ? void 0 : _e.call(this, JSON.parse(args[0]));
                break;
            case "OnFloorClicked":
                (_f = this.onFloorClicked) === null || _f === void 0 ? void 0 : _f.call(this, parseInt(args[0], 10));
                break;
            case "OnCeilingClicked":
                (_g = this.onCeilingClicked) === null || _g === void 0 ? void 0 : _g.call(this, parseInt(args[0], 10));
                break;
            case "OnWallClicked":
                (_h = this.onWallClicked) === null || _h === void 0 ? void 0 : _h.call(this, parseInt(args[0], 10), parseInt(args[1], 10));
                break;
            case "OnObjectClicked":
                (_j = this.onObjectClicked) === null || _j === void 0 ? void 0 : _j.call(this, parseInt(args[0], 10));
                break;
        }
    }
}
exports.LevelPreview = LevelPreview;
;
var Difficulties;
(function (Difficulties) {
    Difficulties[Difficulties["None"] = 0] = "None";
    Difficulties[Difficulties["Easy"] = 1] = "Easy";
    Difficulties[Difficulties["Medium"] = 2] = "Medium";
    Difficulties[Difficulties["Hard"] = 3] = "Hard";
    Difficulties[Difficulties["All"] = 4] = "All";
})(Difficulties || (Difficulties = {}));
;
var WallTextureAndMapFlags;
(function (WallTextureAndMapFlags) {
    WallTextureAndMapFlags[WallTextureAndMapFlags["ShowTextureOnAdjoin"] = 1] = "ShowTextureOnAdjoin";
    WallTextureAndMapFlags[WallTextureAndMapFlags["IlluminatedSign"] = 2] = "IlluminatedSign";
    WallTextureAndMapFlags[WallTextureAndMapFlags["FlipTextureHorizontally"] = 4] = "FlipTextureHorizontally";
    WallTextureAndMapFlags[WallTextureAndMapFlags["ElevatorChangesWallLight"] = 8] = "ElevatorChangesWallLight";
    WallTextureAndMapFlags[WallTextureAndMapFlags["WallTextureAnchored"] = 16] = "WallTextureAnchored";
    WallTextureAndMapFlags[WallTextureAndMapFlags["WallMorphsWithElevator"] = 32] = "WallMorphsWithElevator";
    WallTextureAndMapFlags[WallTextureAndMapFlags["ElevatorScrollsTopEdgeTexture"] = 64] = "ElevatorScrollsTopEdgeTexture";
    WallTextureAndMapFlags[WallTextureAndMapFlags["ElevatorScrollsMainTexture"] = 128] = "ElevatorScrollsMainTexture";
    WallTextureAndMapFlags[WallTextureAndMapFlags["ElevatorScrollsBottomEdgeTexture"] = 256] = "ElevatorScrollsBottomEdgeTexture";
    WallTextureAndMapFlags[WallTextureAndMapFlags["ElevatorScrollsSignTexture"] = 512] = "ElevatorScrollsSignTexture";
    WallTextureAndMapFlags[WallTextureAndMapFlags["HiddenOnMap"] = 1024] = "HiddenOnMap";
    WallTextureAndMapFlags[WallTextureAndMapFlags["NormalOnMap"] = 2048] = "NormalOnMap";
    WallTextureAndMapFlags[WallTextureAndMapFlags["SignTextureAnchored"] = 4096] = "SignTextureAnchored";
    WallTextureAndMapFlags[WallTextureAndMapFlags["DamagePlayer"] = 8192] = "DamagePlayer";
    WallTextureAndMapFlags[WallTextureAndMapFlags["LedgeOnMap"] = 16384] = "LedgeOnMap";
    WallTextureAndMapFlags[WallTextureAndMapFlags["DoorOnMap"] = 32768] = "DoorOnMap";
})(WallTextureAndMapFlags || (WallTextureAndMapFlags = {}));
;
var WallAdjoinFlags;
(function (WallAdjoinFlags) {
    WallAdjoinFlags[WallAdjoinFlags["SkipStepCheck"] = 1] = "SkipStepCheck";
    WallAdjoinFlags[WallAdjoinFlags["BlockPlayerAndEnemies"] = 2] = "BlockPlayerAndEnemies";
    WallAdjoinFlags[WallAdjoinFlags["BlockEnemies"] = 4] = "BlockEnemies";
    WallAdjoinFlags[WallAdjoinFlags["BlockShots"] = 8] = "BlockShots";
})(WallAdjoinFlags || (WallAdjoinFlags = {}));
;
var SectorFlags;
(function (SectorFlags) {
    SectorFlags[SectorFlags["CeilingIsSky"] = 1] = "CeilingIsSky";
    SectorFlags[SectorFlags["SectorIsDoor"] = 2] = "SectorIsDoor";
    SectorFlags[SectorFlags["WallsReflectShots"] = 4] = "WallsReflectShots";
    SectorFlags[SectorFlags["AdjoinAdjacentSkies"] = 8] = "AdjoinAdjacentSkies";
    SectorFlags[SectorFlags["FloorIsIce"] = 16] = "FloorIsIce";
    SectorFlags[SectorFlags["FloorIsSnow"] = 32] = "FloorIsSnow";
    SectorFlags[SectorFlags["SectorIsExplodingWall"] = 64] = "SectorIsExplodingWall";
    SectorFlags[SectorFlags["FloorIsPit"] = 128] = "FloorIsPit";
    SectorFlags[SectorFlags["AdjoinAdjacentPits"] = 256] = "AdjoinAdjacentPits";
    SectorFlags[SectorFlags["ElevatorsCrush"] = 512] = "ElevatorsCrush";
    SectorFlags[SectorFlags["DrawWallsAsSkyPit"] = 1024] = "DrawWallsAsSkyPit";
    SectorFlags[SectorFlags["LowDamage"] = 2048] = "LowDamage";
    SectorFlags[SectorFlags["HighDamage"] = 4096] = "HighDamage";
    SectorFlags[SectorFlags["GasDamage"] = 6144] = "GasDamage";
    SectorFlags[SectorFlags["DenyEnemyTrigger"] = 8192] = "DenyEnemyTrigger";
    SectorFlags[SectorFlags["AllowEnemyTrigger"] = 16384] = "AllowEnemyTrigger";
    SectorFlags[SectorFlags["Subsector"] = 32768] = "Subsector";
    SectorFlags[SectorFlags["SafeSector"] = 65536] = "SafeSector";
    SectorFlags[SectorFlags["Rendered"] = 131072] = "Rendered";
    SectorFlags[SectorFlags["Player"] = 262144] = "Player";
    SectorFlags[SectorFlags["Secret"] = 524288] = "Secret";
})(SectorFlags || (SectorFlags = {}));
;
var ObjectTypes;
(function (ObjectTypes) {
    ObjectTypes[ObjectTypes["Spirit"] = 0] = "Spirit";
    ObjectTypes[ObjectTypes["Safe"] = 1] = "Safe";
    ObjectTypes[ObjectTypes["Sprite"] = 2] = "Sprite";
    ObjectTypes[ObjectTypes["Frame"] = 3] = "Frame";
    ObjectTypes[ObjectTypes["ThreeD"] = 4] = "ThreeD";
    ObjectTypes[ObjectTypes["Sound"] = 5] = "Sound";
})(ObjectTypes || (ObjectTypes = {}));
;
var ObjectDifficulties;
(function (ObjectDifficulties) {
    ObjectDifficulties[ObjectDifficulties["Easy"] = -1] = "Easy";
    ObjectDifficulties[ObjectDifficulties["EasyMedium"] = -2] = "EasyMedium";
    ObjectDifficulties[ObjectDifficulties["EasyMediumHard"] = 0] = "EasyMediumHard";
    ObjectDifficulties[ObjectDifficulties["MediumHard"] = 2] = "MediumHard";
    ObjectDifficulties[ObjectDifficulties["Hard"] = 3] = "Hard";
})(ObjectDifficulties || (ObjectDifficulties = {}));
;

})();

/******/ 	return __webpack_exports__;
/******/ })()
;
});
//# sourceMappingURL=levelPreview.js.map