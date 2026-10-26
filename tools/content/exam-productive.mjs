import {add,build} from './common.mjs';
const put=(exam,section,kind,title,data)=>add('exam',section,kind,title,data,`mock-${exam}`,'linear');
const builds=[
 [1,'Ask about a certificate.','Could you explain ',['how','I can obtain','a replacement certificate'],'?','can I obtain','An embedded question uses subject before modal: I can obtain.'],
 [1,'Describe an earlier booking.','By the time we called, ',['all the available rooms','had already','been reserved'],'.','has already','Past perfect marks the reservation as earlier than the call.'],
 [1,'State what the grant will support.','The grant will enable ',['the students','to conduct','a second experiment'],'.','conducting','Enable takes an object followed by to-infinitive.'],
 [1,'Describe a recent arrival.','The equipment ',['that we ordered','last month','has finally arrived'],'.','have finally arrived','The main subject equipment is uncountable and takes has.'],
 [1,'Express a contrast about the route.','Despite ',['being longer,','the coastal route','was easier to follow'],'.','it was longer,','Despite takes a noun or -ing construction rather than a finite clause alone.'],
 [1,'Ask about the cause of a delay.','What ',['caused','the shipment','to arrive late'],'?','did caused','In a subject question, what directly precedes the verb caused.'],
 [1,'Describe a rule for joining a course.','Only students ',['who have completed','the introductory class','may enroll'],'.','which has completed','Who refers to students and have agrees with the plural subject.'],
 [1,'Explain a recommendation.','The adviser suggested ',['that I','take','a statistics course'],'.','to take','Suggest that is followed here by a clause, not an object plus to-infinitive.'],
 [1,'Predict a result of practice.','The more often you rehearse, ',['the more confident','you are likely','to become'],'.','more confidently','The comparative pattern connects frequency with increased confidence.'],
 [1,'Describe a continuing process.','For the past two weeks, ',['the technicians','have been testing','the new sensors'],'.','has been testing','The plural technicians takes have been in the present perfect continuous.'],
 [2,'Ask where to leave a form.','Do you know ',['where','this form','should be submitted'],'?','should this form','The embedded question retains statement order.'],
 [2,'Describe a delayed opening.','The gallery will remain closed ',['until','the lighting repairs','are complete'],'.','will be complete','A future time clause after until uses present tense.'],
 [2,'Explain a student\'s preparation.','Before applying, she ',['had gathered','all the documents','the office required'],'.','has gathering','Had gathered describes preparation completed before applying.'],
 [2,'Compare two explanations.','This explanation is ',['far clearer','than','the one in the handbook'],'.','more clearer','Clearer is already comparative and does not take more.'],
 [2,'Describe a useful conversation.','Talking to the curator ',['helped me','understand','the exhibition better'],'.','understood','Helped me can be followed by the base form understand.'],
 [2,'Make a polite request.','Would it be possible ',['to extend','the booking','by thirty minutes'],'?','extending','It would be possible is followed by to-infinitive.'],
 [2,'Mention an unexpected benefit.','The task was difficult, ',['but it gave','us an opportunity','to work together'],'.','although it gave','But connects the difficulty with a contrasting benefit.'],
 [2,'Describe a person\'s experience.','The volunteer ',['whose article','we read yesterday','will lead the tour'],'.','who article','Whose marks the relationship between the volunteer and the article.'],
 [2,'Describe an ongoing repair.','The damaged windows ',['are being replaced','while','the building is empty'],'.','is being replaced','Plural windows requires are in the progressive passive.'],
 [2,'State a future achievement.','By the end of the term, ',['we will have','collected enough data','to compare the sites'],'.','we will has','Will have plus participle indicates completion before a future point.'],
 [3,'Ask about an application date.','Can you tell me ',['when','applications for the program','will open'],'?','will applications','The subordinate question follows statement word order.'],
 [3,'Describe a conditional plan.','Unless the weather improves, ',['the group','will postpone','the outdoor survey'],'.','would postponed','Unless with a present condition pairs with a future result here.'],
 [3,'Explain a past mistake.','If I had checked the address, ',['I would not have','sent the package','to the wrong office'],'.','I will not have','An unreal past result uses would not have plus past participle.'],
 [3,'Describe a reason for choosing a room.','We chose the smaller room ',['because it was','quiet enough','for recording'],'.','although it was','Because introduces the room\'s suitability as the reason.'],
 [3,'Mention a returned object.','The keys ',['found near the entrance','have been returned','to their owner'],'.','has been returned','The plural subject keys takes have; found introduces a reduced relative clause.'],
 [3,'Express a preference.','She prefers ',['working in the morning','to studying','late at night'],'.','than studying','Prefer A to B uses parallel -ing forms.'],
 [3,'State what surprised you.','I was surprised ',['by how quickly','the team','resolved the problem'],'.','did the team','An embedded exclamative has ordinary subject-verb order.'],
 [3,'Describe a recent change.','Since the new route opened, ',['journey times','have become','more predictable'],'.','has become','Plural journey times agrees with have in present perfect.'],
 [3,'Explain the value of feedback.','The comments allowed ',['the author','to revise','the unclear paragraph'],'.','revising','Allow takes an object and a to-infinitive.'],
 [3,'Describe an important condition.','No equipment may be removed ',['without','written permission','from the supervisor'],'.','unless written permission','Without takes a noun phrase; unless requires a clause in this construction.']
];
builds.forEach(([exam,prompt,prefix,words,suffix,distractor,explanation],i)=>put(exam,'writing','build-sentence',`Exam sentence ${i%10+1}`,build(prompt,prefix,words,suffix,distractor,explanation)));
const written=[
 [1,'A timetable error','You signed up for a campus photography course. The confirmation lists an evening session, but the published description said it would meet in the afternoon. The evening time conflicts with your part-time job. Write to the course coordinator, Ms. Hart.',['Explain the difference between the advertised and confirmed times.','Describe why the confirmed time is difficult for you.','Request a workable solution.'],'Managing university space','A university has one unused room. Should it become a quiet study room or a space for student clubs?','A quiet room would help students who cannot concentrate in busy shared areas.','A club space would help students meet people and develop interests outside their courses.'],
 [2,'A damaged borrowed poster','Your environmental society borrowed a large illustrated poster from a local nature center for an event. During transport, a corner of the poster was torn. Write to the center\'s education officer, Mr. Reed.',['Explain how the damage happened.','Apologize and describe the current condition.','Offer an appropriate way to put the situation right.'],'University field experiences','Should universities require every student to participate in a short community-based project related to their degree?','Practical projects help students see how their subject connects to real needs.','Compulsory projects may burden students who already combine study with paid work.'],
 [3,'An inaccessible event recording','You attended an online careers workshop and were promised a recording. The link you received does not open, and you want to review a section before an upcoming application deadline. Write to the events administrator, Ms. Silva.',['Identify the workshop and describe the access problem.','Explain why you need the recording soon.','Ask for an alternative way to access the material.'],'Deciding on campus changes','Should a university test major campus changes on a small scale before introducing them everywhere?','A pilot can uncover problems and let students give useful feedback.','Pilots can delay improvements and may not represent how a change works at full scale.']
];
written.forEach(([exam,title,context,requirements,discussion,question,a,b])=>{
 put(exam,'writing','email',title,{context,requirements,instructions:'Write an email in English. Address all three points and use an appropriate tone. You have 7 minutes.'});
 put(exam,'writing','academic-discussion',discussion,{context:`Professor: ${question} Explain and support your view.`,requirements:['Express your position.','Develop it with an explanation or example.','Make an original contribution.'],posts:[{name:'Elena',text:a},{name:'Marcus',text:b}],instructions:'Contribute to the discussion in your own words. You have 10 minutes. An effective response will contain at least 100 words.'});
});
const speech=[
 [1,'Visiting a coastal research station',[
  'Welcome to the research station.', 'The visitor badges are on this table.',
  'Our guide will meet you beside the large aquarium.', 'Please keep the corridor clear while the staff move equipment.',
  'The display upstairs explains how researchers monitor changes in seawater.',
  'If you would like to join the afternoon tour, write your name on the list near reception.',
  'Before entering the outdoor sampling area, put on the protective boots provided and wait for a member of staff.'
 ],'Learning about your local area',[
  'Tell me about something interesting you learned about the place where you live. How did you discover it?',
  'Would you prefer to explore an unfamiliar neighborhood alone or with a guide? Explain your choice.',
  'What could local organizations do to encourage residents to learn about their area\'s history?',
  'Do you think understanding local history can influence decisions about future development? Why or why not?'
 ]],
 [2,'Joining a theater backstage visit',[
  'Our backstage visit begins here.', 'The costume room is along this corridor.',
  'Please leave your drinks outside the rehearsal studio.', 'The stage manager will explain how the lights are controlled.',
  'You may examine the sample costumes after the demonstration has finished.',
  'If a rehearsal is taking place, remain behind the marked line and avoid speaking near the open door.',
  'Before returning to the main hall, check that you have collected your belongings and handed your visitor pass to the guide.'
 ],'Choosing activities outside work or study',[
  'Describe an activity you enjoy when you are not working or studying. How did you become interested in it?',
  'Do you prefer spending free time on a familiar activity or trying something new? Give reasons.',
  'What might help a busy person make time for activities they enjoy?',
  'Should employers or universities provide more opportunities for recreational activities? Explain your opinion.'
 ]],
 [3,'Exploring a materials discovery center',[
  'Welcome to the discovery center.', 'The first display is beside the stairs.',
  'You can compare the samples by touching their surfaces.', 'Please return each sample to its labelled space after examining it.',
  'The next demonstration shows why some materials bend more easily than others.',
  'If you would like further information, our volunteers can suggest experiments that you can try at home.',
  'Before moving to the final gallery, make sure everyone in your group has finished using the equipment at this station.'
 ],'Solving everyday problems',[
  'Tell me about an everyday problem you solved recently. What steps did you take?',
  'When facing an unfamiliar problem, do you prefer trying your own solution first or asking for advice? Why?',
  'How can people learn useful problem-solving skills from mistakes?',
  'Should schools give students more open-ended tasks with several possible solutions? Explain your view.'
 ]]
];
speech.forEach(([exam,context,prompts,topic,interview])=>{
 prompts.forEach((prompt,i)=>put(exam,'speaking','listen-repeat',`${context} · ${i+1}`,{context,prompt,scenePosition:i+1,responseSeconds:[8,8,10,10,10,12,12][i],instructions:'Listen once. Repeat the sentence exactly when recording begins. There is no preparation time.'}));
 interview.forEach((prompt,i)=>put(exam,'speaking','interview',`${topic} · ${i+1}`,{context:`You are participating in a research interview about ${topic.toLowerCase()}.`,prompt,scenePosition:i+1,responseSeconds:45,instructions:'Listen to the question, then respond with relevant details. You have 45 seconds and no preparation time.'}));
});
