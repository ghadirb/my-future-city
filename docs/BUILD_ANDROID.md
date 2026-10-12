# ساخت APK اندروید

## وضعیت فعلی
Workflow ساخت (`.github/workflows/build-android.yml`) نوشته شده ولی **تا وقتی Secretهای مجوز Unity تنظیم نشوند اجرا نمی‌شود** (job به‌صورت Skipped نمایش داده می‌شود، نه سبز). هیچ APK ساخته نشده است.

## کاری که شما باید یک‌بار انجام دهید (Unity Personal)
مطابق مستندات GameCI:
1. Unity Hub را نصب و وارد حساب Unity شوید.
2. `Preferences > Licenses > Add > Get a free personal license` (حتماً این مرحله را انجام دهید تا فایل `.ulf` ساخته شود).
3. فایل `Unity_lic.ulf` را پیدا کنید:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. در GitHub: `Settings > Secrets and variables > Actions` سه Secret بسازید:
   - `UNITY_LICENSE` = کل محتوای فایل `.ulf`
   - `UNITY_EMAIL` = ایمیل حساب Unity
   - `UNITY_PASSWORD` = رمز حساب Unity
   (برای لایسنس Plus/Pro به‌جای `UNITY_LICENSE` از `UNITY_SERIAL` استفاده کنید.)
5. `Actions > Build Android > Run workflow`.

## خروجی
Artifact با نام `MyFutureCity-android-apk` (نگهداری ۱۴ روز). نصب: APK را روی گوشی کپی و نصب کنید (نصب از منابع ناشناس را برای مرورگر/فایل‌منیجر مجاز کنید).

## تنظیمات ساخت (در `BuildScript.cs`)
- Unity `6000.3.11f1` (در `ProjectSettings/ProjectVersion.txt` و workflowها یکسان نگه دارید)
- شناسه بسته: `com.myfuturecity.game` — **پیش از انتشار فروشگاهی عوض کنید**
- IL2CPP، فقط ARM64، حداقل API 24، گرافیک GLES3، بدون هیچ مجوز اندرویدی (آفلاین)
- ساخت آزمایشی با امضای Debug است. امضای Release در `docs/RELEASE_CHECKLIST.md`.

## نکته‌ها
- صحنه‌ها در زمان بیلد توسط `SceneGenerator` ساخته می‌شوند؛ بیلد اگر اعتبارسنج خطا بدهد متوقف می‌شود.
- نسخه Unity باید ایمیج Docker متناظر در GameCI داشته باشد؛ اگر اولین اجرا خطای «image not found» داد، نسخه را به یک پچ دیگر 6000.3.x تغییر دهید.
