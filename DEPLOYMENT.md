# BarakMeetings Deployment Guide (Render.com)

This guide walks you through deploying BarakMeetings to Render.com for production.

## Prerequisites

1. A [Render](https://render.com) account (free tier available)
2. A [GitHub](https://github.com) account with the repo pushed
3. Gmail App Password (you already have: `hwrg mtox awkz awfq`)
4. Your existing Neon PostgreSQL database

---

## Step 1: Push to GitHub

First, push your code to GitHub:

```bash
cd /Users/barakbraun/Desktop/BarakMeetings
git add -A
git commit -m "Initial commit - BarakMeetings booking platform"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/BarakMeetings.git
git push -u origin main
```

---

## Step 2: Deploy Backend API

### 2.1 Create Web Service

1. Go to [Render Dashboard](https://dashboard.render.com)
2. Click **New +** → **Web Service**
3. Connect your GitHub repo (BarakMeetings)
4. Configure:
   - **Name:** `barakmeetings-api`
   - **Root Directory:** `backend`
   - **Runtime:** `Docker`
   - **Instance Type:** `Free`

### 2.2 Add Environment Variables

In the **Environment** section, add these variables:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Host=ep-divine-flower-ah9vb8is-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_DB_PASSWORD;SSL Mode=Require` |
| `Jwt__Secret` | `BarakMeetings_Production_Secret_Key_That_Is_Very_Long_And_Secure_2026!` |
| `Email__SmtpHost` | `smtp.gmail.com` |
| `Email__SmtpPort` | `587` |
| `Email__SmtpUser` | `bbarak94@gmail.com` |
| `Email__SmtpPassword` | `hwrg mtox awkz awfq` |
| `Email__FromEmail` | `bbarak94@gmail.com` |
| `Email__FromName` | `BarakMeetings` |
| `Email__EnableSsl` | `true` |
| `Email__IsEnabled` | `true` |
| `Cors__AllowedOrigins__0` | `https://barakmeetings-app.onrender.com` |

### 2.3 Deploy

Click **Create Web Service**. Wait for the build to complete (5-10 minutes for first deploy).

Your API URL will be: `https://barakmeetings-api.onrender.com`

### 2.4 Verify Backend

Visit: `https://barakmeetings-api.onrender.com/health`

Should return:
```json
{"status":"healthy","timestamp":"..."}
```

---

## Step 3: Deploy Frontend

### 3.1 Create Static Site

1. Click **New +** → **Static Site**
2. Connect the same GitHub repo
3. Configure:
   - **Name:** `barakmeetings-app`
   - **Root Directory:** `frontend`
   - **Build Command:** `npm ci && npm run build`
   - **Publish Directory:** `dist`

### 3.2 Add Environment Variables

| Key | Value |
|-----|-------|
| `VITE_API_URL` | `https://barakmeetings-api.onrender.com/api` |

### 3.3 Add Rewrite Rule

In **Redirects/Rewrites** section, add:
- **Source:** `/*`
- **Destination:** `/index.html`
- **Action:** `Rewrite`

This enables SPA routing.

### 3.4 Deploy

Click **Create Static Site**. Wait for build to complete.

Your frontend URL will be: `https://barakmeetings-app.onrender.com`

---

## Step 4: Update CORS (if needed)

If your frontend URL is different, go back to the **API service** and update:
- `Cors__AllowedOrigins__0` = your actual frontend URL

---

## Step 5: Verify Everything

### Test Health Check
```
https://barakmeetings-api.onrender.com/health
```

### Test Swagger
```
https://barakmeetings-api.onrender.com/swagger
```

### Test Frontend
```
https://barakmeetings-app.onrender.com
```

### Login
- **Email:** `admin@fitstudio.demo`
- **Password:** `Demo123!`

### Test Invitation Flow
1. Go to Team Members
2. Invite yourself with a real email
3. Check your inbox for the invitation email

---

## Environment Variables Quick Reference

### Backend (Web Service)

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Your Neon connection string |
| `Jwt__Secret` | Long random string (64+ chars) |
| `Email__SmtpHost` | `smtp.gmail.com` |
| `Email__SmtpPort` | `587` |
| `Email__SmtpUser` | `bbarak94@gmail.com` |
| `Email__SmtpPassword` | `hwrg mtox awkz awfq` |
| `Email__FromEmail` | `bbarak94@gmail.com` |
| `Email__FromName` | `BarakMeetings` |
| `Email__EnableSsl` | `true` |
| `Email__IsEnabled` | `true` |
| `Cors__AllowedOrigins__0` | Your frontend URL |

### Frontend (Static Site)

| Variable | Value |
|----------|-------|
| `VITE_API_URL` | `https://YOUR-API.onrender.com/api` |

---

## Demo Accounts

| Role | Email | Password |
|------|-------|----------|
| **Owner** | admin@fitstudio.demo | Demo123! |
| **Staff** | sarah@fitstudio.demo | Demo123! |
| **Staff** | mike@fitstudio.demo | Demo123! |

**Public Booking URL:** `https://barakmeetings-app.onrender.com/book/fitstudio`

---

## Troubleshooting

### "Service unavailable" on first request
Render free tier services spin down after 15 minutes of inactivity. First request takes 30-60 seconds to wake up.

### Build fails
- Check that `Root Directory` is set correctly (`backend` or `frontend`)
- Check the build logs for specific errors

### CORS errors
- Make sure `Cors__AllowedOrigins__0` matches your frontend URL exactly
- Include `https://` and no trailing slash

### Emails not sending
- Verify App Password is correct (no spaces when entering in Render)
- Check `Email__IsEnabled=true`

### Database connection fails
- Verify Neon connection string format
- Make sure SSL Mode is set

---

## Costs

### Render Free Tier
- 750 hours/month for web services
- Static sites are free
- Services sleep after 15 min inactivity
- Cold starts take 30-60 seconds

### Render Paid ($7/month per service)
- No sleep/spin down
- Faster performance
- Better for demos

---

## Production URLs

After deployment:
- **Frontend:** `https://barakmeetings-app.onrender.com`
- **API:** `https://barakmeetings-api.onrender.com/api`
- **Health:** `https://barakmeetings-api.onrender.com/health`
- **Swagger:** `https://barakmeetings-api.onrender.com/swagger`
- **Booking:** `https://barakmeetings-app.onrender.com/book/fitstudio`

---

*Last Updated: January 2026*
