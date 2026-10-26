import {add,q,items} from './common.mjs';
const sets=[
 [1,[
 ['Could you point me toward the registration desk?','It is just beyond the glass doors.','Registration was quite straightforward.','I pointed out two spelling errors.','The desk was made locally.'],
 ['I have not received the reading list yet.','The lecturer said she would upload it this afternoon.','I read that book during the summer.','The list contains several names.','Afternoon classes are usually quieter.'],
 ['Would you like me to save you a place in the queue?','Thanks, I will be there in a moment.','The queue was longer yesterday.','I saved the document on my laptop.','There are several places to eat nearby.'],
 ['This article seems more relevant than the first one.','I agree; it addresses our research question directly.','The first bus leaves at seven.','I put the article in a blue folder.','We researched the opening hours.'],
 ['Have you heard whether the outdoor event is going ahead?','Yes, the organizer confirmed it this morning.','I heard an interesting song earlier.','The event poster is very colorful.','I usually go ahead of my friends.'],
 ['I am afraid I cannot stay for the whole meeting.','That is all right; we can discuss your part first.','The meeting room is upstairs.','I stayed in a small hotel.','There is a hole in this folder.'],
 ['How soon can the technician look at the broken scanner?','She has an opening after lunch.','The scan is quite clear.','I looked at the display yesterday.','Lunch costs less for students.'],
 ['These instructions leave out an important step.','We should ask the instructor before continuing.','The staircase has twelve steps.','I left my umbrella at home.','The instructions are on recycled paper.'],
 ['Where can I refill my water bottle?','There is a fountain beside the lift.','My bottle holds half a liter.','The water was cold yesterday.','I bought it in a sports shop.'],
 ['Can I borrow your ruler for a moment?','Sure, here you are.','The ruler is thirty centimeters long.','I borrowed a novel last week.','The moment passed quickly.'],
 ['What time does the shop close today?','At six this evening.','I live close to the shop.','It sells notebooks and pens.','Today is quite busy.'],
 ['Thank you for carrying that heavy bag.','No problem at all.','The bag is dark green.','It has two pockets.','I carried my lunch yesterday.'],
 ['Is there a lift in this building?','Yes, around the corner on your right.','I lifted the box carefully.','The building opened last year.','My room is larger than this one.'],
 ['I think I left my scarf in the classroom.','Let us check there before it is locked.','The scarf was a gift.','The classroom seats forty people.','I think the lesson was useful.'],
 ['Shall we take a short break?','Yes, I could use a few minutes to stretch.','The report has a short introduction.','I broke a cup yesterday.','We took the earlier train.'],
 ['Would the earlier train give us enough time to change platforms?','It should, provided the first service arrives on schedule.','The platform has a new sign.','I changed my password recently.','Earlier reports discussed a different station.'],
 ['I would not rule out revising the introduction once the results are clearer.','So we should treat the current version as provisional.','The introduction is printed in black.','The room has several clear windows.','I followed the rules at the entrance.'],
 ['The curator seemed hesitant about lending the object.','Perhaps she is concerned about how it would be transported.','I lent my neighbor a ladder.','The curator arrived before noon.','The object was mentioned on page three.']
 ]],
 [2,[
 ['Do you know who is coordinating the poster session?','I believe Dr. Lane is in charge.','The posters are printed in color.','My session lasted an hour.','I know the room is warm.'],
 ['I cannot attend the briefing at noon.','There is another session at three, if that helps.','The brief report has four pages.','Noon is in the middle of the day.','I attended a concert last month.'],
 ['Could you check whether my name is on the list?','Certainly. How do you spell your surname?','The list is on white paper.','My name is quite short.','I checked the weather earlier.'],
 ['We have more survey responses than we expected.','Then we may need extra time to analyze them.','The survey is about transport.','I expected the room to be warmer.','We responded to the invitation.'],
 ['Would you prefer a window seat or an aisle seat?','An aisle seat, please; I like being able to get up easily.','The window needs cleaning.','I prefer travelling in summer.','The seat is covered in blue fabric.'],
 ['I thought you were presenting after the break.','They changed the order because another speaker was delayed.','The presentation includes a map.','I broke the task into smaller parts.','The order arrived in a box.'],
 ['This room echoes too much for our recording.','Let us try the smaller room with the curtains.','The recording is twelve minutes long.','I tried a new recipe yesterday.','The room number is on the door.'],
 ['Could we use your notes to fill the gap in our timeline?','Of course, but please check the uncertain dates against another source.','The notes are in my handwriting.','The timeline is a meter long.','I filled my bottle after lunch.'],
 ['Where should I put these chairs?','Against the wall, please.','They are made of wood.','I sat on one earlier.','There are ten of them.'],
 ['Are you free on Tuesday afternoon?','Yes, after two o\'clock.','Tuesday comes after Monday.','The afternoon was sunny.','Admission is free for children.'],
 ['Can you open the window a little?','Certainly, is that enough?','The shop opens at nine.','I have a little notebook.','The window frame is white.'],
 ['Which bus goes to the hospital?','The number twelve stops outside it.','The hospital is quite large.','I go there once a month.','The bus has comfortable seats.'],
 ['I cannot find my glasses.','Have you looked on your desk?','The glass is nearly empty.','I found the lecture interesting.','My desk is made from oak.'],
 ['Would you like some tea?','Yes, please, without sugar.','Tea grows in several countries.','I liked the film we saw.','The cup is on the shelf.'],
 ['How was your first day at work?','Busy, but everyone was very welcoming.','Work begins at eight.','The first page has a title.','I work near the station.'],
 ['The sample is smaller than planned, though not necessarily unusable.','Then we should explain its limitations when presenting the findings.','The sample jar has a red lid.','I planned to arrive earlier.','The findings are in the second folder.'],
 ['Would postponing the decision give us a better basis for comparison?','Possibly, if the remaining estimates arrive in time.','The decision was printed yesterday.','I compared the two room numbers.','Time passes quickly during a meeting.'],
 ['She sounded unconvinced that the extra feature was worth the cost.','We may need stronger evidence of its practical benefit.','The feature is described on the cover.','I heard a strange sound outside.','The cost is written in pencil.']
 ]],
 [3,[
 ['Is the application form available online?','Yes, you can download it from the admissions page.','I applied last autumn.','The form has three pages.','Online lectures are convenient.'],
 ['I am having trouble keeping track of all the references.','A reference manager might help you organize them.','The references are about plants.','I kept the receipt in my pocket.','The track was wet after the rain.'],
 ['Could we move our appointment to the morning?','I can offer you a slot at ten.','The appointment lasted twenty minutes.','I moved to this city last year.','Morning light is brighter here.'],
 ['The first draft is ready for you to review.','Great, I will send you comments by tomorrow.','I reviewed that topic in class.','The paper is thick and smooth.','The first train was crowded.'],
 ['Have you found someone to cover your shift?','Yes, Morgan has agreed to take it.','The cover is made from cardboard.','I found a coin near the door.','My shift is usually busy.'],
 ['This diagram is hard to read from the back of the room.','We could enlarge the labels before presenting.','The room is on the ground floor.','I read the book last week.','The back door is painted green.'],
 ['Would you mind checking the total one more time?','Not at all; I will add the figures again.','The figures are written neatly.','Time is shown on the screen.','I checked in at the hotel.'],
 ['I am not sure the proposed date works for everyone.','Let us send a quick poll before confirming it.','The proposal has six pages.','Dates are useful in a calendar.','Everyone enjoyed the previous event.'],
 ['Where is the nearest cash machine?','Across the street beside the pharmacy.','I paid in cash yesterday.','The machine is quite new.','The street is longer than this one.'],
 ['May I use this computer?','Yes, I have finished with it.','The computer has a large screen.','I used a pencil for the drawing.','The desk has two drawers.'],
 ['When will the books be ready to collect?','Tomorrow afternoon.','The collection includes poetry.','The books are heavy.','I read them during the holiday.'],
 ['Could you turn the music down?','Sorry, I did not realize it was disturbing you.','The music is from a film.','I turned left at the crossing.','The speaker is beside the lamp.'],
 ['How do I get to the garden entrance?','Follow this path and take the second left.','The garden has many flowers.','I got here by train.','The entrance sign is wooden.'],
 ['Do I need to bring a pen?','No, we provide all the materials.','The pen writes in blue.','I brought my lunch today.','The material feels soft.'],
 ['I enjoyed your talk this morning.','Thank you. I am glad you found it interesting.','Morning talks start at nine.','The talk lasted half an hour.','I enjoyed walking here.'],
 ['The revised estimate leaves less room for unexpected expenses.','We should probably keep a larger contingency reserve.','The room was revised on the map.','I expected the meeting to end sooner.','The estimate has a neat heading.'],
 ['I take your point, but would that approach work on a larger scale?','We would need to test it with more participants before assuming so.','The scale is beside the sink.','I approached the building from the north.','The point is marked with a red dot.'],
 ['The evidence supports the explanation, although it does not settle the issue.','So we should present it as a possibility rather than a certainty.','The evidence is in a clear folder.','I settled into my new room quickly.','The issue has an attractive cover.']
 ]]
];
for(const [exam,rows] of sets) rows.forEach(([text,correct,...wrong],i)=>{
 const stage=i<8?'router':i<15?'lower':'upper';
 add('exam','listening','choose-response',`Spoken exchange ${i+1}`,{text,instructions:'Listen once and choose the most appropriate response.',questions:items(q('Choose the most appropriate response.',correct,wrong,`"${correct}" directly addresses the speaker's request or implied meaning; the other options do not respond appropriately.`))},`mock-${exam}`,stage);
});
