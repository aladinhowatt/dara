# MNEMOSYNE — โครงสร้างคัทซีน (วิดีโอ + ตัวเลือก) → ลูปเกม

โค้ดอยู่ที่ `Dara\Assets\Scripts\Mnemosyne\` (Unity 6000.3.12f1) สถานะ: **คอมไพล์ผ่านและตรรกะข้อมูลทดสอบแล้ว แต่ยังไม่ได้ทดสอบเล่นจริงในเอดิเตอร์** (ดูหัวข้อ 6)

## 1. ภาพรวม

```
Chapter (FlowData.Chapter1)  =  รายการ Segment เรียงลำดับ
 ├─ Cutscene "intro_nara"   → CutscenePlayer เล่นวิดีโอ → ตัวเลือก → Effect เปลี่ยนค่าใน GameState
 ├─ DayLoop 1               → ลูปเดิม: คาบเช้า → ช่วงอิสระ (กดสำรวจ/คุย ใช้ AP + เวลา) → คาบบ่าย → ... → กลางคืน
 ├─ DayLoop 2..5
 └─ (เพิ่ม Cutscene แทรกระหว่างวันได้)
```

`GameController` ทำหน้าที่เดียวคือเดิน segment ทีละอัน (`StartFlow` / `NextSegment`) แล้วส่งต่อให้ตัวเล่นที่เหมาะสม คัทซีนจบแล้วเรียก callback เพื่อไป segment ถัดไป

## 2. ไฟล์

| ไฟล์ | หน้าที่ |
|---|---|
| `Effect.cs` | `Effect` = ผลของตัวเลือก (Ap, Time, Suspicion, Composure/Wit/Strength, Affection รายคน, Codex, Flags, NextDayApPenalty) พร้อม `Apply(GameState)` · `Requirement` = เงื่อนไขปลดล็อกตัวเลือก (สเตตัส, ความสนิท, flag) |
| `CutsceneData.cs` | `CutsceneDef` → `CutsceneNode` (วิดีโอ/ข้อความ + คำถาม + `PauseAt`) → `ChoiceDef` (ข้อความ, `Fx`, `Need`, `Next`) และคลังข้อมูล `CutsceneData.All` |
| `FlowData.cs` | `SegmentDef` (`Cutscene` / `DayLoop`) และลำดับของบท `FlowData.Chapter1` |
| `CutscenePlayer.cs` | เล่นวิดีโอเต็มจอ หยุดที่จุดตัดสินใจ แสดงตัวเลือก ใช้ Effect แล้วไปโหนดถัดไป ไม่ผูกกับ `GameController` (เรียก `Play(def, state, onDone)`) |
| `UiKit.cs` | ตัวช่วยสร้าง uGUI (Panel / Text / Button) |
| `GameState.cs` | เพิ่ม `Flags` (เก็บทุกการเลือกเป็น `choice:<cutscene>:<node>:<choice>` ใช้เป็นเงื่อนไขในภายหลัง) |

## 3. กฎของระบบคัทซีน

- โหนดหนึ่งเล่นได้ 3 แบบ: วิดีโอ, ข้อความล้วน (`Lines`), หรือวิดีโอแล้วค้างเฟรมสุดท้ายพร้อมตัวเลือก
- ตัวเลือกปรากฏเมื่อคลิปจบ หรือที่ `PauseAt` วินาที (ถ้ากำหนด) กด Space/คลิกเพื่อข้าม จะกระโดดไปที่จุดตัดสินใจ **ไม่ข้ามตัวเลือก**
- ตัวเลือกที่ยังไม่ผ่านเงื่อนไขแสดงจางและบอกเงื่อนไข (เช่น `[Wit 3]`) ตามสเปกเดิม
- ผู้เล่นไม่เห็นตัวเลขผลลัพธ์ มีเฉพาะ `Debug.Log` และ event `ChoiceMade`
- วิดีโอวางที่ `Assets\StreamingAssets\Video\` ระบุเป็นพาธสัมพัทธ์ เช่น `Video/nara_intro.mp4`

## 4. ลูปเกมหลังคัทซีน

ใช้ลูปเดิมของโปรโตไทป์โดยไม่แก้กติกา: ช่วงอิสระมีเวลา (`FreeTime`) และ AP ทุกการกระทำหักทั้งสอง ดูตารางตัวแปรใน `MNEMOSYNE_night_events_spec.md`
**ข้อควรระวัง:** `StartDay()` เติม AP ใหม่ทุกวัน ดังนั้นผลที่ต้องการให้ส่งถึงวันถัดไปจากคัทซีนที่อยู่ก่อน DayLoop ต้องใช้ `NextDayApPenalty` ไม่ใช่ `Ap`

## 5. วิธีเพิ่มคัทซีนใหม่

1. วางวิดีโอใน `Assets\StreamingAssets\Video\`
2. ใน `CutsceneData` สร้าง `CutsceneDef` เพิ่มโหนดและตัวเลือก ลงทะเบียนใน `All`
3. ใน `FlowData.Chapter1` แทรก `SegmentDef.Cutscene("id")` ตรงตำแหน่งที่ต้องการ (เช่น ก่อน/หลัง DayLoop ใดก็ได้)

## 6. สถานะการตรวจสอบ

- คอมไพล์ในเอดิเตอร์ผ่าน ไม่มี error/warning ใน console
- ทดสอบตรรกะข้อมูลแบบไม่เปิดเกม: ตัวเลือกปลดล็อกตามเงื่อนไข (Wit 3), Effect เปลี่ยนค่า Suspicion/Composure/Wit/Affection/Codex/Flags ถูกต้อง
- **ยังไม่ได้ทดสอบเล่นจริง:** ตอนทดสอบ หน้าต่าง Unity ไม่ได้อยู่ด้านหน้า เฟรมไม่เดิน (`isFocused=False`) จึงยังไม่เห็นวิดีโอเล่น ปุ่มตัวเลือกขึ้น และการต่อเข้าลูปวัน ต้องเปิดเล่นเองหนึ่งรอบ (Game_Prototype → เริ่มเกม)
- ข้อความตัวเลือก/บทในคัทซีนตัวอย่างเป็นของจำลอง (ใช้ชื่อ "กอฟ" ตามคลิปทดสอบ ซึ่งยังไม่ตรงกับบทแนะนำ "Chapter 1 : กวิน" ในโปรโตไทป์)
- ลบโค้ดเล่นวิดีโอเปิดเรื่องแบบเก่าใน `GameController` ออกแล้ว (แทนด้วยระบบนี้) ใช้ `nara_intro.mp4` เดิมเป็นคลิปแรก และเพิ่ม `nara_after_name.mp4` (คลิปฉากต่อ 6 วินาที)

## 6.5 ส่วนเสริมเอนจินสำหรับ choice map รอบ 1 (เพิ่มแล้ว)

- **ข้ามโหนดตามเงื่อนไข:** `Branch(Requirement, next)` ใส่ได้ทั้งที่ `CutsceneNode.Branches` และ `ChoiceDef.Branches` ใช้ branch แรกที่เงื่อนไขผ่าน ไม่ผ่านใช้ `Next` · โหนดที่ไม่มีวิดีโอ/ข้อความ/ตัวเลือกคือ "gate" ที่ route อย่างเดียว
- **Requirement ใหม่:** `HasItem`, `Equipped`, `Counter`, `SuspicionAtLeast/Below`, `Not`, `All`, `Any`
- **ไอเทม:** `GameState.Items` (เริ่มด้วย `has_pin`) + `Equipped` (Prep Slots 3 ช่อง) · `Effect.Give/Take/Unflag/Count` · รายการไอเทมใน `ItemData.cs`
- **โหนดพิเศษ:** `Fx` (ใช้ตอนเข้าโหนด), `Title` (การ์ดข้อความ), `PrepSlots` (หน้าจอเลือกของพก), `Minigame` + `Counter` + `PassFx/FailFx` (เล่นมินิเกมแล้วบวกตัวนับ)
- **มินิเกม:** `Minigame.cs` มี 3 ด่านตัวอย่าง (hide/run/stand) กลไกชั่วคราวเป็นแถบจับจังหวะ ของที่พกช่วยขยายโซน
- **แยกวัน/คืน:** `SegmentDef.DayLoop(n)` จบเมื่อหมดช่วงอิสระ 2 · `SegmentDef.Night(n, cutsceneId?)` เล่นคัทซีนกลางคืน หรือสรุปคืนแบบเดิมถ้าไม่ระบุ
- คัทซีนทดสอบ `engine_demo` (ยังไม่อยู่ใน `FlowData`) ใช้ทุกฟีเจอร์ข้างต้น
- ทดสอบแล้ว: คอมไพล์ผ่าน, ตรรกะ branch/requirement/ไอเทมผ่านการรันโค้ดทดสอบ · **ยังไม่ได้เล่นจริงในหน้าต่างเกม** (UI มินิเกม/Prep Slots/การเปลี่ยนวัน→คืน)

## 7. ที่ยังไม่ได้ทำ / ตัดสินใจต่อ

1. ข้อมูลคัทซีนเขียนในโค้ด (เหมือน `GameData`) ถ้าอยากให้ทีมเขียนบทแก้ได้โดยไม่คอมไพล์ ควรย้ายเป็น JSON ใน StreamingAssets
2. ตัวนับเวลาจำกัดในตัวเลือก (timed choice) ยังไม่มี
3. เซฟ/โหลด: `GameState` พร้อมแล้ว (มี `Flags`) แต่ยังไม่มีระบบบันทึก
4. คัทซีนกลางคืน (อังคาร–พฤหัส) และคืนวันศุกร์ ยังไม่ผูกเข้า `FlowData`
5. ข้อเสนอเรื่อง `PauseAt` กลางคลิป ยังไม่ได้ลองกับคลิปจริง
