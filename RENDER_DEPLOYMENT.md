# Render এ Deploy করার জন্য গাইড

## পূর্ব-প্রয়োজনীয় জিনিস
- Render একাউন্ট (https://render.com)
- GitHub একাউন্ট এবং রিপোজিটরি
- আপনার প্রজেক্ট Render এর জন্য প্রস্তুত

## স্টেপ ১: GitHub এ Push করুন
```bash
git add .
git commit -m "Render deployment configuration"
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

**Runtime:** `Docker` অথবা নীচের মেনুয়াল সেটিংস:
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

**Environment** ট্যাব এ যান এবং নিম্নলিখিত ভেরিয়েবল যোগ করুন:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:${PORT}` |
| `DATABASE_URL` | MySQL কানেকশন স্ট্রিং (নিচে দেখুন) |

## স্টেপ ৬: Database সেটআপ (MySQL)

### বিকল্প ১: Render এর MySQL ব্যবহার করুন
1. Render Dashboard এ Database সেকশনে যান
2. "New MySQL" তৈরি করুন
3. Automatic connectivity - Render স্বয়ংক্রিয়ভাবে DATABASE_URL সেট করবে

### বিকল্প ২: বাহ্যিক MySQL Server ব্যবহার করুন
```
mysql://username:password@hostname:port/databasename
```

## স্টেপ ৭: Deploy করুন

1. সমস্ত সেটিংস পরীক্ষা করুন
2. **"Create Web Service" বাটনে ক্লিক করুন**
3. Render স্বয়ংক্রিয়ভাবে আপনার কোড বিল্ড এবং ডিপ্লয় করবে

## ডিপ্লয়মেন্ট পরে

- আপনার অ্যাপ্লিকেশন লাইভ হবে URL এ: `https://your-service-name.onrender.com`
- লগ দেখতে Render Dashboard এ "Logs" ট্যাব চেক করুন
- যেকোনো ত্রুটির জন্য GitHub এ পুশ করুন - এটি স্বয়ংক্রিয়ভাবে পুনঃডিপ্লয় হবে

## সমস্যা সমাধান

### "Connection string not found" ত্রুটি
- নিশ্চিত করুন DATABASE_URL environment variable সেট আছে
- appsettings.Production.json চেক করুন

### পোর্ট সম্পর্কিত ত্রুটি
- PORT environment variable স্বয়ংক্রিয়ভাবে Render দ্বারা সেট হয়
- ASPNETCORE_URLS সঠিকভাবে কনফিগার করা আছে তা নিশ্চিত করুন

### Database সংযোগ ব্যর্থতা
- DATABASE_URL সংযোগ স্ট্রিং সঠিক তা যাচাই করুন
- MySQL সার্ভার অনলাইন এবং অ্যাক্সেসযোগ্য তা নিশ্চিত করুন
- পোর্ট নম্বর (সাধারণত 3306) খোলা আছে তা চেক করুন

## অতিরিক্ত টিপস

- Render এ সম্পূর্ণ বিনামূল্যে টায়ার উপলব্ধ (CPU থ্রটলিং সহ)
- Database বার্ষিক তথ্য বৃদ্ধির জন্য নিয়মিত ব্যাকআপ নিন
- Production ডাটাবেসের জন্য দৃঢ় পাসওয়ার্ড ব্যবহার করুন
- HTTPS স্বয়ংক্রিয়ভাবে সক্ষম থাকে
