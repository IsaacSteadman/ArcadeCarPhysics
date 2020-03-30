// Adapted from Crockford's JSON.parse (see https://github.com/douglascrockford/JSON-js)
// This version adds support for NaN, -Infinity and Infinity.

// tweaked once more to use 'const' and 'let'
// added JSON.stringifyMore which can stringify NaN, -Infinity and Infinity


const escapee = {
  '"': '"',
  '\\': '\\',
  '/': '/',
  b: '\b',
  f: '\f',
  n: '\n',
  r: '\r',
  t: '\t'
};
let text;
let at; // The index of the current character
let ch; // The current character
const error = function (m) {
  throw new SyntaxError(`${m}: ${text.length > 4096 ? '<JSON>' : text} ${at}`);
};
const next = function () {
  ch = text.charAt(at++);
  return ch;
};
const check = function (c) {
  if (c !== ch) {
    error("Expected '" + c + "' instead of '" + ch + "'");
  }
  ch = text.charAt(at++);
};
const number = function () {
  let string = '';
  if (ch === '-') {
    string = '-';
    check('-');
  }
  if (ch === 'I') {
    check('I');
    check('n');
    check('f');
    check('i');
    check('n');
    check('i');
    check('t');
    check('y');
    return -Infinity;
  }
  /* eslint-disable no-unmodified-loop-condition */
  while (ch >= '0' && ch <= '9') {
    string += ch;
    next();
  }
  if (ch === '.') {
    string += '.';
    while (next() && ch >= '0' && ch <= '9') {
      string += ch;
    }
  }
  if (ch === 'e' || ch === 'E') {
    string += ch;
    next();
    if (ch === '-' || ch === '+') {
      string += ch;
      next();
    }
    while (ch >= '0' && ch <= '9') {
      string += ch;
      next();
    }
  }
  /* eslint-enable no-unmodified-loop-condition */
  return +string;
};
const string = function () {
  let string = '';
  if (ch === '"') {
    while (next()) {
      if (ch === '"') {
        next();
        return string;
      }
      if (ch === '\\') {
        next();
        if (ch === 'u') {
          let uffff = 0;
          for (let i = 0; i < 4; ++i) {
            let hex = parseInt(next(), 16);
            if (!isFinite(hex)) {
              break;
            }
            uffff = uffff * 16 + hex;
          }
          string += String.fromCharCode(uffff);
        } else if (escapee[ch]) {
          string += escapee[ch];
        } else {
          break;
        }
      } else {
        string += ch;
      }
    }
  }
  error('Bad string');
};
const white = function () { // Skip whitespace.
  /* eslint-disable no-unmodified-loop-condition */
  while (ch && ch <= ' ') {
    next();
  }
  /* eslint-enable no-unmodified-loop-condition */
};
const word = function () {
  switch (ch) {
    case 't':
      check('t');
      check('r');
      check('u');
      check('e');
      return true;
    case 'f':
      check('f');
      check('a');
      check('l');
      check('s');
      check('e');
      return false;
    case 'n':
      check('n');
      check('u');
      check('l');
      check('l');
      return null;
    case 'N':
      check('N');
      check('a');
      check('N');
      return NaN;
    case 'I':
      check('I');
      check('n');
      check('f');
      check('i');
      check('n');
      check('i');
      check('t');
      check('y');
      return Infinity;
  }
  error("Unexpected '" + ch + "'");
};
const array = function () {
  var array = [];
  if (ch === '[') {
    check('[');
    white();
    if (ch === ']') {
      check(']');
      return array;   // empty array
    }
    /* eslint-disable no-unmodified-loop-condition */
    while (ch) {
      array.push(value());
      white();
      if (ch === ']') {
        check(']');
        return array;
      }
      check(',');
      white();
    }
    /* eslint-enable no-unmodified-loop-condition */
  }
  error('Bad array');
};
const object = function () {
  let object = {};
  if (ch === '{') {
    check('{');
    white();
    if (ch === '}') {
      check('}');
      return object;   // empty object
    }
    /* eslint-disable no-unmodified-loop-condition */
    while (ch) {
      const key = string();
      white();
      check(':');
      if (Object.hasOwnProperty.call(object, key)) {
        error('Duplicate key "' + key + '"');
      }
      object[key] = value();
      white();
      if (ch === '}') {
        check('}');
        return object;
      }
      check(',');
      white();
    }
    /* eslint-enable no-unmodified-loop-condition */
  }
  error('Bad object');
};
const value = function () {
  white();
  switch (ch) {
    case '{':
      return object();
    case '[':
      return array();
    case '"':
      return string();
    case '-':
      return number();
    default:
      return ch >= '0' && ch <= '9' ? number() : word();
  }
};
export function parseMore(source: string, reviver): any {
  text = source;
  at = 0;
  ch = ' ';
  const result = value();
  white();
  if (ch) {
    error('Syntax error');
  }
  return typeof reviver === 'function'
    ? (function walk(holder, key) {
      const value = holder[key];
      if (value && typeof value === 'object') {
        for (let k in value) {
          if (Object.prototype.hasOwnProperty.call(value, k)) {
            const v = walk(value, k);
            if (v !== undefined) {
              value[k] = v;
            } else {
              delete value[k];
            }
          }
        }
      }
      return reviver.call(holder, key, value);
    }({ '': result }, ''))
    : result;
};
export function parsePiece(source: string, reviver?: (this: any, key: string, value: any) => any, pos?: number): [any, number] {
  text = source;
  at = pos == null ? 0 : pos;
  ch = ' ';
  const result = value();
  pos = at;
  return [(typeof reviver === 'function'
    ? (function walk(holder, key) {
      const value = holder[key];
      if (value && typeof value === 'object') {
        for (let k in value) {
          if (Object.prototype.hasOwnProperty.call(value, k)) {
            const v = walk(value, k);
            if (v !== undefined) {
              value[k] = v;
            } else {
              delete value[k];
            }
          }
        }
      }
      return reviver.call(holder, key, value);
    }({ '': result }, ''))
    : result), pos];
};
const repeatStr = function (str, n) {
  let rtn = '';
  for (let c = 0; c < n; ++c) {
    rtn += str;
  }
  return rtn;
};
export function stringify(value, k, replacer, space, indent) {
  if (typeof replacer === 'function') {
    value = replacer.call(this, value, k);
  }
  if (value instanceof Number) {
    value = value.valueOf();
  } else if (value instanceof String) {
    value = value.valueOf();
  } else if (value instanceof Boolean) {
    value = value.valueOf();
  }
  if (value === null) {
    return 'null';
  } else if (value === undefined) {
    return null;
  } else if (typeof value === 'boolean') {
    return JSON.stringify(value);
  } else if (typeof value === 'string') {
    return JSON.stringify(value);
  } else if (typeof value === 'number') {
    if (isFinite(value)) {
      return JSON.stringify(value);
    } else if (isNaN(value)) {
      return 'NaN';
    } else {
      return value > 0 ? 'Infinity' : '-Infinity';
    }
  } else if (typeof value === 'object' && value instanceof Array) {
    if (value.length === 0) return '[]';
    ++indent;
    let str = '[';
    if (space != null) str += '\n' + repeatStr(space, indent);
    str += stringify.call(value, value[0], 0, replacer, space, indent);
    for (let c = 1; c < value.length; ++c) {
      const str1 = stringify.call(value, value[c], c, replacer, space, indent);
      str += ',';
      if (space != null) str += '\n' + repeatStr(space, indent);
      str += str1 == null ? 'null' : str1;
    }
    --indent;
    if (space != null) str += '\n' + repeatStr(space, indent);
    str += ']';
    return str;
  } else if (typeof value === 'function' || value instanceof Function) {
    return null;
  }
  ++indent;
  const strings = Object.keys(value).filter(function (k) {
    return typeof k === 'string';
  }).map(function (k) {
    const rtn = stringify.call(value, value[k], k, replacer, space, indent);
    if (rtn != null) {
      return JSON.stringify(k) + (space != null ? ': ' : ':') + rtn;
    }
    return rtn;
  }).filter(function (v) {
    return v !== undefined;
  });
  if (strings.length === 0) return '{}';
  let str = '{';
  const indentStr = space == null ? '' : ('\n' + repeatStr(space, indent));
  str += indentStr + strings.join(',' + indentStr);
  --indent;
  if (space != null) str += '\n' + repeatStr(space, indent);
  str += '}';
  return str;
};
export function stringifyMore(value, replacer, space) {
  if (typeof space === 'number' || space instanceof Number) {
    space = repeatStr(' ', space);
  } else if (typeof space !== 'string' && !(space instanceof String)) {
    space = null;
  }
  return stringify.call({ '': value }, value, '', replacer, space, 0);
};


