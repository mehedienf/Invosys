# Render এ Deploy করার জন্য গাইড

## পূর্ব-প্রয়োজনীয় জিনিস
- Render একাউন্ট (https://render.com)
- GitHub একাউন্ট এবং রিপোজিটরি
- আপনার প্রজেক্ট Render এর জন্য প্রস্তুত

## ডাটাবেস সম্পর্কে
এই প্রজেক্ট **SQLite** ব্যবহার করে যা একটি অভ্যন্তরীণ ফাইল-ভিত্তিক ডাটাবেস। কোনো বাহ্যিক MySQL সার্ভারের প্রয়োজন নেই!

- **উন্নয়নে**: `inventory.db` ফাইল প্রজেক্ট ডিরেক্টরিতে থাকে
- **Render এ**: `/data/inventory.db` এ থাকে (persistent disk এ)

## স্টেপ ১: GitHub এ Push করুন
```bash
git add .
git commit -m "Switch to SQLite internal database"
git push origin main
```

## স্টেপ ২: Render এ নতুন Web Service তৈরি করুন

1. **Render.com এ লগইন করুন**
2. **Dashboard এ যান**
3. **"+ New" বাটনে ক্লিক করুন**
4. **"Web Service" নির্বাচন করুন**

## স্টেপ ৩: Repository সংযুক্ত করুন

- আপনার GitHub রিপোজিটরি সংযোগ করুন
- ব্র্যাঞ্চ নির্বাচন করুন: `main`

## স্টেপ ৪: ডিপ্লয়মেন্ট কনফিগারেশন

**Name:** `inventory-and-sales-tracker` (বা আপনার পছন্দের নাম)

**Runtime:** `Docker` 
- Dockerfile এর মাধ্যমে স্বয়ংক্রিয় ডিপ্লয় হবে

**অথবা Manual Configuration:**
- Runtime: `.NET`
- Build Command: 
  ```
  dotnet publish -c Release -o ./out
  ```
- Start Command:
  ```
  cd ./out && dotnet Inventory_and_Sales_Tracker.dll
  ```

## স্টেপ ৫: Environment Variables যোগ করুন

**Environment** ট্যাব এ যান এবং নিম্নলিখিত যোগ করুন:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:${PORT}` |

## স্টেপ ৬: Persistent Disk সেটআপ (গুরুত্বপূর্ণ!)

ডাটাবেস সংরক্ষণের জন্য একটি Disk প্রয়োজন:

1. **Render Dashboard এ Web Service তৈরি করুন**
2. **"Disks" ট্যাব এ যান**
3. **"Create Disk" বাটনে ক্লিক করুন**
   - Name: `inventory-db`
   - Mount path: `/data`
   - Size: `1GB` (যথেষ্ট হবে)

## স্টেপ ৭: Deploy করুন

1. সমস্ত সেটিংস পরীক্ষা করুন
2. **"Create Web Service" বাটনে ক্লিক করুন**
3. Render স্বয়ংক্রিয়ভাবে আপনার কোড বিল্ড এবং ডিপ্লয় করবে

## ডিপ্লয়মেন্ট পরে

- আপনার অ্যাপ্লিকেশন লাইভ হবে URL এ: `https://your-service-name.onrender.com`
- লগ দেখতে Render Dashboard এ "Logs" ট্যাব চেক করুন
- যেকোনো ত্রুটির জন্য GitHub এ পুশ করুন - এটি স্বয়ংক্রিয়ভাবে পুনঃডিপ্লয় হবে

## সমস্যা সমাধান

### "inventory.db not found" ত্রুটি
- প্রথম ডিপ্লয়মেন্টে ডাটাবেস স্বয়ংক্রিয়ভাবে তৈরি হবে
- Program.cs এ `context.Database.EnsureCreated()` চলে

### ডাটা হারিয়ে যাচ্ছে?
- নিশ্চিত করুন যে Persistent Disk সঠিকভাবে `/data` এ মাউন্ট করা আছে
- Render Dashboard → Web Service → Disks চেক করুন

### Application শুরু হচ্ছে না
- Logs চেক করুন: Dashboard → Logs
- Connection string সঠিক তা নিশ্চিত করুন
- `appsettings.Production.json` এ `Data Source=/data/inventory.db` আছে কিনা দেখুন

## অতিরিক্ত টিপস

### উন্নয়নে স্থানীয় ব্যবহার
```bash
# .NET CLI দিয়ে চালান
dotnet run

# Database স্বয়ংক্রিয়ভাবে তৈরি হবে
# Data Source: inventory.db (প্রজেক্ট ফোল্ডারে)
```

### ডাটাবেস ব্যাকআপ
SQLite হওয়ায় সহজ ব্যাকআপ:
1. Render Dashboard থেকে `/data/inventory.db` ডাউনলোড করুন
2. স্থানীয়ভাবে সংরক্ষণ করুন

### Render এ বিনামূল্যে টায়ার
- Web Service: বিনামূল্যে (CPU throttling সহ)
- Persistent Disk: 10GB পর্যন্ত বিনামূল্যে

## MySQL-এ ফিরে যেতে চাইলে

যদি পরবর্তীতে MySQL ব্যবহার করতে চান:
1. `.csproj` ফাইলে `Pomelo.EntityFrameworkCore.MySql` পুনরায় যোগ করুন
2. `Program.cs` এ `UseMySql` দিয়ে পরিবর্তন করুন
3. Connection string আপডেট করুন

