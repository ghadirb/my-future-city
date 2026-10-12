# معماری

```
Core (بدون Unity)           Runtime (Unity)                 Editor
-------------------         ------------------------        ----------------------
MissionData / Validator     GameContext (نگهدارندهٔ سرویس‌ها)  SceneGenerator
SaveData (+Sanitize)        SaveStore (JSON + .bak)          ProjectValidator
Wallet                      MissionDataLoader (Resources)    BuildScript
CityProgressService         Player/Camera/Interaction/NPC
MissionService              HudController / MainMenu / UIKit
Recommender                 BuildingVisual (نمای ساختمان)
PersianShaper
```

## تصمیم‌ها
- **منطق جدا از Unity:** مأموریت‌ها، پاداش، ذخیره و آبادانی در `Core` هستند تا با Mono هم تست شوند.
- **داده‌محور:** مأموریت‌ها در `missions.json`. افزودن مأموریت = ویرایش JSON + قرار دادن اشیای صحنه با `targetId` متناظر (اعتبارسنج ناهمخوانی را گزارش می‌دهد).
- **مأموریت:** هدف‌ها به ترتیب؛ فقط یک مأموریت فعال؛ رها کردن بدون جریمه؛ پاداش کامل فقط بار اول، تکرار فقط `repeatCoins`.
- **آبادانی:** پیشرفت عددی ساختمان (۰..۲۰۰) در ذخیره؛ وضعیت از آن مشتق می‌شود: متروک / در حال بازسازی / بازسازی‌شده (۱۰۰) / فعال (۱۵۰). `BuildingVisual` ظاهر را عوض می‌کند.
- **رندر:** Built-in Render Pipeline با شیدر `Mobile/Diffuse` و `Unlit/Color` (بدون وابستگی SRP)؛ سایه خاموش به‌صورت پیش‌فرض. گذار به URP بعداً ممکن است.
- **ورودی:** Input Manager قدیمی (Touch + کیبورد برای تست)، بدون پکیج Input System.
- **UI:** با کد ساخته می‌شود (uGUI، `Text` قدیمی + فونت Vazirmatn). متن فارسی از `PersianShaper` عبور می‌کند (اتصال حروف با Presentation Forms، لام‌الف، RTL، ارقام فارسی). 
- **تولید صحنه:** `SceneGenerator` هر بار صحنه‌ها را از نو می‌سازد (idempotent)؛ صحنه و متریال تولیدی در گیت نیستند. پس از اولین باز کردن در Unity فایل‌های `.meta` ساخته می‌شوند؛ آن‌ها را commit کنید.
- **ذخیره:** `persistentDataPath/save.json` + پشتیبان؛ نوشتن بعد از رویدادهای مهم و هنگام pause/quit؛ فایل خراب → پشتیبان → ذخیرهٔ جدید.
- **حریم خصوصی:** آفلاین، بدون مجوز اندروید، بدون حساب کاربری، تحلیل علایق کاملاً محلی.

## محدودیت‌های شناخته‌شده
- شخصیت‌ها/ساختمان‌ها با primitive ساخته شده‌اند (placeholder)؛ انیمیشن راه‌رفتن ساده و رویه‌ای است.
- متن بلند فارسی با `Wrap` تقریبی (بر اساس تعداد حروف) شکسته می‌شود، نه عرض واقعی.
- صدا/موسیقی هنوز نیست؛ گزینهٔ بی‌اثر هم در تنظیمات نمایش داده نمی‌شود.
