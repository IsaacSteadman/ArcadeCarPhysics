import { openSync, writeSync, readFileSync, existsSync, readFile, writeFile, mkdirSync, } from 'fs';
import * as express from 'express';
import { promisify } from 'util';
import { resolve, } from 'path';
import { createServer } from 'http';
import * as bodyParser from 'body-parser';
import { CORS, DEBUG, PORT } from './consts';
import { parsePiece } from './JsonParser';

const app = express();
const server = createServer(app);

if (CORS) {
  app.use(function (req, res, next) {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET,PUT,POST,DELETE');
    res.header('Access-Control-Allow-Headers', 'Content-Type');
    next();
  });
}

app.use('/arcade-survey/', express.static(resolve(__dirname, '../dist')));
app.use('/', express.static(resolve(__dirname, '../dist')));
// app.use('/icons', express.static(resolve(__dirname, '../icons')));
app.use(bodyParser.json({ limit: '50mb' }));
const logFileFd = openSync(resolve(__dirname, '../data.json'), 'a');

/* app.use(function(req, res, next) {
  req['rawBody'] = '';
  req.setEncoding('utf8');

  req.on('data', function(chunk) { 
    req['rawBody'] += chunk;
  });

  req.on('end', function() {
    next();
  });
}); */

if (DEBUG) {
  app.use(function (req, res, next) {
    console.log(`${req.method} ${req.url}`);
    console.log('req.query = ' + JSON.stringify(req.query, null, 2));
    console.log(`req.body = ${JSON.stringify(req.body, null, 2)}`);
    next();
  });
}

const pReadFile = promisify(readFile);
const surveyFilename = resolve(__dirname, '../data.json');

const getFn = async function (req, res) {
  res.status(200).sendFile(surveyFilename);
  // const data = await pReadFile(resolve(__dirname, '../data.json'));
  // res.status(200).send(data);
  // res.status(200).json({type: 'WARNING', message: 'this endpoint is only designed to be PUTed or POSTed'});
};

const putFn = (req, res) => {
  // console.log(req['rawBody']);
  // req.body = JSON.parse(req['rawBody']);
  // console.log('PUT body =', req.body);
  console.log('PUT @ ' + new Date())
  const data = Object.assign({ timeStamp: Date.now() }, req.body);
  const buf = Buffer.from(JSON.stringify(data) + '\n', 'utf8');
  writeSync(logFileFd, buf);
  res.status(200).json({ type: 'SUCCESS', message: 'data logged' });
};

app.get('/survey', getFn);
app.put('/survey', putFn);
app.get('/arcade-survey/survey', getFn);
interface SurveyEntry {
  timeStamp: number;
  surveyData: {
    Question_0: [
      string
    ],
    Question_1: [
      string
    ],
    Question_2: [
      string
    ],
    Question_3: [
      string
    ],
    Question_4: [
      string
    ],
    Question_5: [
      string
    ]
  };
  parameters: {
    lapNumber: number;
    fps: number;
    resolutionMultiple: number;
    q_len: number;
    fps_var: number;
  };
  systemInfo: {
    deviceType: string,
    deviceModel: string,
    deviceUniqueIdentifier: string,
    operatingSystem: string,
    processorType: string
  };
  uuid: string;
}
function toCsvWord(str: string) {
  if (str.indexOf('"') === -1 && str.indexOf(',') === -1) {
    return str;
  }
  return `"${str.split('"').join('""')}"`;
}
async function getJsonArrayData(): Promise<SurveyEntry[]> {
  const data = await pReadFile(surveyFilename, 'utf8');
  let pos = 0;
  let val;
  const dataParsed: SurveyEntry[] = [];
  while (pos < data.length) {
    [val, pos] = parsePiece(data, null, pos);
    dataParsed.push(val);
    while (pos < data.length && /\s/.test(data.charAt(pos))) {
      ++pos;
    }
  }
  return dataParsed;
}
app.get('/arcade-survey/survey.csv', async function (req, res) {
  const dataParsed = await getJsonArrayData();
  const rows: string[][] = dataParsed.map(function (entry): string[] {
    const {
      timeStamp,
      surveyData: {
        Question_0,
        Question_1,
        Question_2,
        Question_3,
        Question_4,
        Question_5
      },
      parameters: {
        lapNumber,
        fps,
        resolutionMultiple,
        q_len,
        fps_var
      },
      systemInfo: {
        deviceType,
        deviceModel,
        deviceUniqueIdentifier,
        operatingSystem,
        processorType
      },
      uuid: sessionUuid
    } = entry;
    return ['' + timeStamp, Question_0[0], Question_1[0], Question_2[0], Question_3[0], Question_4[0], Question_5[0], '' + lapNumber, '' + fps, '' + resolutionMultiple, '' + q_len, '' + fps_var, deviceType, deviceModel, deviceUniqueIdentifier, operatingSystem, processorType, sessionUuid];
  });
  rows.unshift(['timeStamp', 'Question_0', 'Question_1', 'Question_2', 'Question_3', 'Question_4', 'Question_5', 'lapNumber', 'fps', 'resolutionMultiple', 'q_len', 'fps_var', 'deviceType', 'deviceModel', 'deviceUniqueIdentifier', 'operatingSystem', 'processorType', 'sessionUuid']);
  res.setHeader('Content-Type', 'text/csv;charset=utf-8');
  res.status(200).send(Buffer.from(rows.map(row => {
    return row.map(toCsvWord).join(',');
  }).join('\r\n'), 'utf8'));
});
app.get('/arcade-survey/survey.json', async function (req, res) {
  res.status(200).json(await getJsonArrayData());
});
app.put('/arcade-survey/survey', putFn);
console.log(`listening on port: ${PORT}`);
server.listen(PORT);
