# شهر آینده من — My Future City

بازی سه‌بعدی Low Poly ماجراجویی و آبادانی شهر برای اندروید (Unity، زبان فارسی).
سند طراحی کامل: [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md)

## وضعیت (صادقانه)
| مورد | وضعیت |
|---|---|
| منطق هسته (مأموریت، پاداش، ذخیره، آبادانی، پیشنهاد، شکل‌دهی متن فارسی) | نوشته و با ۴۴ تست واقعاً اجرا شده (Mono) |
| کد Unity (بازیکن، دوربین، تعامل، UI، تولید صحنه، بیلد) | نوشته شده؛ **هنوز داخل Unity کامپایل/اجرا نشده** |
| APK | **ساخته نشده** — نیازمند Secret مجوز Unity، رجوع به [`docs/BUILD_ANDROID.md`](docs/BUILD_ANDROID.md) |
| تست روی گوشی | انجام نشده |

جزئیات مرحله‌ها: [`docs/PROGRESS.md`](docs/PROGRESS.md)

## ساختار
- `Assets/_Game/Scripts/Core` — منطق خالص C# (بدون وابستگی به Unity، تست‌پذیر)
- `Assets/_Game/Scripts/Runtime` — اسکریپت‌های بازی
- `Assets/_Game/Scripts/Editor` — تولید خودکار صحنه، اعتبارسنج، اسکریپت بیلد
- `Assets/_Game/Resources/Data/missions.json` — مأموریت‌ها (داده‌محور)
- `Assets/_Game/Tests/EditMode` — تست‌ها
- `tools/` — اسکریپت‌های بررسی بدون Unity

## اجرای محلی
1. پروژه را با Unity **6000.3.11f1** باز کنید.
2. منوی `Tools > My Future City > Generate Scenes` (صحنه‌ها هر بار از نو ساخته می‌شوند و در گیت نیستند).
3. صحنه `MainMenu` را Play کنید. (کیبورد: WASD، دکمه E، چرخش دوربین با کلیک راست)

## بررسی سریع بدون Unity
```bash
python3 tools/validate_repo.py
./tools/run-logic-tests.sh        # نیاز: mono-mcs
```
