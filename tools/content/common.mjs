import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';
export const entries = [];
export const id = value => { const h = createHash('sha256').update(value).digest('hex'); return `${h.slice(0,8)}-${h.slice(8,12)}-4${h.slice(13,16)}-8${h.slice(17,20)}-${h.slice(20,32)}`; };
export function add(pool, section, kind, title, data = {}, examId = '', stage = 'linear') {
  const n = entries.filter(e => e.pool === pool && e.section === section && e.examId === examId).length + 1;
  const key = `${pool}-${examId}-${section}-${n}`;
  const entry = { id: id(`toefl-original-2026-10-v1:${key}`), pool, examId, stage, section, kind, title, ...data };
  if (section === 'listening' || section === 'speaking') entry.audioFile = entry.id.replaceAll('-', '') + '.wav';
  entries.push(entry); return entry;
}
// Rotate option positions so an authored correct answer at index 0 does not become a test-taking cue.
let rotation = 0;
export function q(prompt, correct, distractors, explanation) {
  const shift = rotation++ % 4;
  const texts = [correct, ...distractors];
  return { id: 0, prompt, prefix: '', missingLetters: 0,
    options: Array.from({length:4}, (_, i) => ({ id: String.fromCharCode(65+i), text: texts[(i-shift+4)%4] })),
    answer: String.fromCharCode(65+shift), explanation };
}
export function items(...questions) { return questions.map((q,i) => ({...q,id:i+1})); }
export function cloze(first, middle, last) {
  const words = middle.split(' '), questions = [];
  for (let i=1; questions.length<10; i+=2) {
    const word = words[i]?.match(/^[A-Za-z]+/)?.[0];
    if (!word || word.length<2) throw Error('Invalid C-test word '+words[i]);
    const keep = Math.floor(word.length/2), n=questions.length+1;
    questions.push({id:n,prompt:'Complete the missing letters.',prefix:word.slice(0,keep),missingLetters:word.length-keep,options:[],answer:word.slice(keep),explanation:`The complete word is "${word}" in this sentence: ${middle}`});
    words[i]=`{{${n}}}`+words[i].slice(word.length);
  }
  return {text:`${first} ${words.join(' ')} ${last}`,questions};
}
export function build(prompt, prefix, words, suffix, distractor, explanation) {
  const digest=createHash('sha256').update('tile-ids:'+prompt).digest();
  const ids=[];
  for(let i=0;i<=words.length;i++){let value=digest.readUInt16BE(i*2)+1;while(ids.includes(value))value++;ids.push(value);}
  const tiles = words.map((text,i)=>({id:ids[i],text})); tiles.push({id:ids[words.length],text:distractor});
  return {prompt,prefix,suffix,tiles:tiles.sort((a,b)=>((a.id*17)%101)-((b.id*17)%101)),order:ids.slice(0,words.length),explanation};
}
export function save(name, pool) {
  const rows = entries.filter(e=>e.pool===pool);
  if(pool==='exam') {
    const sections=['reading','listening','writing','speaking'], stages=['router','lower','upper','linear'];
    const kinds=['complete-words','daily-life','academic-passage','choose-response','conversation','announcement','academic-talk','build-sentence','email','academic-discussion','listen-repeat','interview'];
    rows.sort((a,b)=>a.examId.localeCompare(b.examId)||sections.indexOf(a.section)-sections.indexOf(b.section)||stages.indexOf(a.stage)-stages.indexOf(b.stage)||kinds.indexOf(a.kind)-kinds.indexOf(b.kind));
  }
  const dir='src/ToeflExam.Infrastructure/ContentBank'; mkdirSync(dir,{recursive:true});
  writeFileSync(`${dir}/${name}.json`,JSON.stringify(rows,null,2)+'\n');
  for(const section of ['reading','listening','writing','speaking']) console.log(name,section,rows.filter(e=>e.section===section).reduce((n,e)=>n+(e.questions?.length||1),0));
}
