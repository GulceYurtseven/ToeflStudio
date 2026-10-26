import {add} from './common.mjs';
const scenes=[
 ['Community observatory tour',[
  'Welcome to our observatory.',
  'The ticket counter is beside the entrance.',
  'You can leave your bags in the lockers downstairs.',
  'Please switch off bright screens before entering the viewing room.',
  'The evening presentation explains how astronomers study distant planets.',
  'If clouds cover the sky, our guide will demonstrate the telescope using recorded images.',
  'Before joining the outdoor session, collect a warm jacket and check the meeting point on your ticket.'
 ]],
 ['At the community food workshop',[
  'Our cooking workshop starts here.',
  'Clean aprons are hanging beside the door.',
  'Please wash your hands before touching the fresh ingredients.',
  'The instructor will show you how to hold each tool safely.',
  'Everyone should label their container before placing it in the refrigerator.',
  'If you have a food allergy, speak to the instructor before your group begins preparing anything.',
  'When the workshop ends, return the equipment to its shelf and wipe your table with a clean cloth.'
 ]]
];
scenes.forEach(([context,prompts])=>prompts.forEach((prompt,i)=>add('practice','speaking','listen-repeat',`${context} · ${i+1}`,{context,prompt,scenePosition:i+1,responseSeconds:[8,8,10,10,10,12,12][i],instructions:'You are following a guided visit. Listen once and repeat exactly what you hear. Start speaking when recording begins.'})));
const interviews=[
 ['Finding your way',[
  'Tell me about a place you visited for the first time. How did you find your way around?',
  'When you need directions, do you prefer asking a person or using a map? Explain your preference.',
  'What could a university do to help new students feel confident finding unfamiliar buildings?',
  'Some people believe relying on navigation apps weakens our ability to understand places. Do you agree? Why?'
 ]],
 ['Sharing everyday objects',[
  'Describe an object you have borrowed from someone. Why did you need it?',
  'Would you rather borrow something you rarely use or buy your own? Explain your answer.',
  'What rules would make a shared equipment service work well for a neighborhood?',
  'Do you think schools should teach children how to share responsibility for common resources? Why or why not?'
 ]],
 ['Explaining an idea',[
  'Think of a time you explained something to another person. What were you explaining?',
  'Do you find an example or a diagram more helpful when learning a difficult idea? Why?',
  'How should someone adapt an explanation when the listener has no background in the topic?',
  'Should researchers spend more time explaining their work to people outside their field? Support your view.'
 ]],
 ['Using public spaces',[
  'Describe a public place where you enjoy spending time. What do you usually do there?',
  'Do you prefer a lively public space or a quiet one? Give reasons for your choice.',
  'What change could make a public space in your area more welcoming to different people?',
  'When cities have limited money, should they improve existing public spaces or build new ones? Explain your opinion.'
 ]],
 ['Managing a group task',[
  'Tell me about a task you completed with other people. What was your role?',
  'Do you prefer having a clear leader or sharing leadership in a group? Why?',
  'If group members disagree about how to proceed, what is a useful way to reach a decision?',
  'Should universities assess teamwork skills separately from the final product of a group project? Why or why not?'
 ]],
 ['Making things last',[
  'Describe something you own that you have used for a long time. Why have you kept it?',
  'When an item stops working, do you usually try to repair it or replace it? Explain.',
  'What would encourage more people to learn basic repair skills?',
  'Should companies have greater responsibility for helping customers repair old products? Support your answer.'
 ]],
 ['Choosing information',[
  'Where do you usually look when you want to learn about a topic that interests you?',
  'Do you prefer a short overview or a detailed explanation when approaching something new? Why?',
  'What would you do if two sources gave conflicting explanations of the same issue?',
  'Should evaluating online information be a required part of every university degree? Explain your view.'
 ]],
 ['Changing routines',[
  'Tell me about a small change you have made to your daily routine. What prompted it?',
  'Do you prefer making changes gradually or starting a completely new routine at once? Why?',
  'What can help someone maintain a useful habit when their schedule becomes busy?',
  'Some people think workplaces should allow employees to design their own daily schedules. Do you agree? Explain.'
 ]],
 ['Learning from feedback',[
  'Describe a time someone gave you useful feedback. How did it help?',
  'Do you prefer receiving feedback in writing or through a conversation? Why?',
  'How can a teacher give critical feedback without discouraging a learner?',
  'Should students have more opportunities to revise work after receiving feedback? Explain your reasons.'
 ]]
];
interviews.forEach(([context,prompts])=>prompts.forEach((prompt,i)=>add('practice','speaking','interview',`${context} · ${i+1}`,{context:`You are taking part in a research interview about ${context.toLowerCase()}.`,prompt,responseSeconds:45,scenePosition:i+1,instructions:'Listen to the interviewer. Respond in English with relevant details and examples. There is no preparation time.'})));
