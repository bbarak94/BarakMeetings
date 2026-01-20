# BarakMeetings Deployment Guide

This guide walks you through deploying BarakMeetings to Railway for production.

## Prerequisites

1. A [Railway](https://railway.app) account (free tier available)
2. A [GitHub](https://github.com) account with the repo pushed
3. A Gmail account for sending emails
4. Your existing Neon PostgreSQL database (or create a new one on Railway)

---

## Step 1: Set Up Gmail App Password

Before deploying, you need a Gmail App Password for sending invitation emails:

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable **2-Step Verification** if not already enabled
3. Go to **App passwords** (search for it in security settings)
4. Click "Select app" → "Other (Custom name)"
5. Name it "BarakMeetings"
6. Click **Generate**
7. **Save the 16-character password** - you'll need it for Railway

---

## Step 2: Deploy Backend to Railway

### 2.1 Create New Project

1. Log in to [Railway](https://railway.app)
2. Click **New Project** → **Deploy from GitHub repo**
3. Select your BarakMeetings repository
4. Railway will auto-detect the Dockerfile

### 2.2 Configure Root Directory

1. Go to **Settings** → **Source**
2. Set **Root Directory** to: `backend`
3. This tells Railway to build from the backend folder

### 2.3 Add Environment Variables

Go to **Variables** tab and add these (click "New Variable" for each):

```
# Database (use your existing Neon connection string)
ConnectionStrings__DefaultConnection=Host=ep-divine-flower-ah9vb8is-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=Require

# JWT Secret (generate a random 64+ character string)
Jwt__Secret=BarakMeetings_Production_Secret_Key_That_Is_At_Least_64_Characters_Long_For_Maximum_Security_2026!

# Email Configuration (Gmail)
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=your-email@gmail.com
Email__SmtpPassword=YOUR_16_CHAR_APP_PASSWORD
Email__FromEmail=your-email@gmail.com
Email__FromName=BarakMeetings
Email__EnableSsl=true
Email__IsEnabled=true

# CORS (will update after frontend deploy)
Cors__AllowedOrigins__0=http://localhost:5173
```

### 2.4 Generate Domain

1. Go to **Settings** → **Networking**
2. Click **Generate Domain**
3. Note your backend URL: `https://xxx.railway.app`

### 2.5 Deploy

Railway will automatically build and deploy. Watch the **Deployments** tab for logs.

---

## Step 3: Deploy Frontend to Railway

### 3.1 Create Another Service

1. In the same Railway project, click **+ New** → **GitHub Repo**
2. Select the same BarakMeetings repo again

### 3.2 Configure Root Directory

1. Go to **Settings** → **Source**
2. Set **Root Directory** to: `frontend`

### 3.3 Add Environment Variables

Go to **Variables** tab:

```
# API URL (use your backend Railway URL from Step 2.4)
VITE_API_URL=https://your-backend-xxx.railway.app/api
```

### 3.4 Generate Domain

1. Go to **Settings** → **Networking**
2. Click **Generate Domain**
3. Note your frontend URL: `https://xxx.railway.app`

---

## Step 4: Update CORS Settings

Go back to your **backend** service and update the CORS variable:

```
Cors__AllowedOrigins__0=https://your-frontend-xxx.railway.app
Cors__AllowedOrigins__1=http://localhost:5173
```

The backend will automatically redeploy with the new settings.

---

## Step 5: Verify Deployment

### Check Backend Health
Visit: `https://your-backend-xxx.railway.app/health`

Should return:
```json
{"status":"healthy","timestamp":"2026-01-20T..."}
```

### Check Swagger API Docs
Visit: `https://your-backend-xxx.railway.app/swagger`

### Check Frontend
Open `https://your-frontend-xxx.railway.app` in your browser.

### Test Login
Use the default admin credentials:
- **Email:** `admin@demo.com`
- **Password:** `Admin123!`

---

## Environment Variables Reference

### Backend Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=xxx;Database=xxx;...` |
| `Jwt__Secret` | JWT signing key (64+ chars) | `your-long-random-string...` |
| `Email__SmtpHost` | SMTP server | `smtp.gmail.com` |
| `Email__SmtpPort` | SMTP port | `587` |
| `Email__SmtpUser` | Gmail address | `you@gmail.com` |
| `Email__SmtpPassword` | Gmail App Password | `xxxx xxxx xxxx xxxx` |
| `Email__FromEmail` | Sender email | `you@gmail.com` |
| `Email__FromName` | Sender display name | `BarakMeetings` |
| `Email__EnableSsl` | Use SSL/TLS | `true` |
| `Email__IsEnabled` | Enable email sending | `true` |
| `Cors__AllowedOrigins__0` | Frontend URL | `https://xxx.railway.app` |

### Frontend Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `VITE_API_URL` | Backend API URL | `https://xxx.railway.app/api` |

---

## Demo Accounts

After deployment, these accounts are automatically seeded:

| Role | Email | Password | Description |
|------|-------|----------|-------------|
| **Owner** | admin@demo.com | Admin123! | Full access, billing |
| **Staff** | sarah@fitstudio.demo | Demo123! | Yoga Instructor |
| **Staff** | mike@fitstudio.demo | Demo123! | Personal Trainer |
| **Staff** | emily@fitstudio.demo | Demo123! | Massage Therapist |

**Tenant Slug:** `fitstudio`
**Public Booking URL:** `https://your-frontend/book/fitstudio`

---

## Troubleshooting

### "JWT Secret not configured"
- Ensure `Jwt__Secret` is set and at least 32 characters
- Check there are no typos in the variable name

### "Database connection failed"
- Verify `ConnectionStrings__DefaultConnection` format
- For Neon: `Host=xxx.neon.tech;Database=neondb;Username=xxx;Password=xxx;SSL Mode=Require`
- Make sure the password doesn't have special characters that need escaping

### "CORS error in browser"
- Update `Cors__AllowedOrigins__0` to match your frontend URL exactly
- Include the full URL with `https://`
- No trailing slash

### "Email not sending"
1. Verify you're using a Gmail **App Password**, not your regular password
2. Check `Email__IsEnabled=true`
3. Make sure 2FA is enabled on your Gmail account
4. Check Railway logs for specific email errors

### "Frontend shows blank page"
- Open browser DevTools (F12) → Console tab
- Usually means `VITE_API_URL` is incorrect
- Make sure URL ends with `/api`

### "Health check passes but app doesn't work"
- Check Railway deployment logs for startup errors
- Verify all environment variables are set correctly
- Try redeploying by clicking "Redeploy" in Railway

---

## Custom Domain (Optional)

To use your own domain (e.g., `app.yourdomain.com`):

1. In Railway, go to **Settings** → **Networking**
2. Click **+ Custom Domain**
3. Enter your domain
4. Add the CNAME record to your DNS provider:
   - Type: CNAME
   - Name: app (or your subdomain)
   - Value: (provided by Railway)
5. Wait for SSL certificate (usually 5-10 minutes)

---

## Monitoring & Logs

Railway provides built-in monitoring:

- **Logs**: Click on a service → "Logs" tab for real-time logs
- **Metrics**: "Metrics" tab shows CPU, memory, network
- **Deployments**: History of all deployments with rollback option

### Viewing Logs
```bash
# Install Railway CLI
npm install -g @railway/cli

# Login
railway login

# View logs
railway logs
```

---

## Costs

### Railway Free Tier
- 500 hours of execution per month
- $5 credit to start
- Shared CPU/RAM
- Services sleep after inactivity

### Railway Hobby Plan ($5/month)
- Always-on services (no sleeping)
- 8 GB RAM
- More CPU
- Recommended for production demos

### Neon Free Tier
- 0.5 GB storage
- 1 compute hour/day on free tier
- Sufficient for demos

---

## Quick Reference

### Local Development
```bash
cd BarakMeetings
npm run dev
```
- Frontend: http://localhost:5173
- Backend: http://localhost:5001
- Swagger: http://localhost:5001/swagger

### Production URLs (after deploy)
- Frontend: `https://your-frontend-xxx.railway.app`
- Backend API: `https://your-backend-xxx.railway.app/api`
- Health Check: `https://your-backend-xxx.railway.app/health`
- Swagger: `https://your-backend-xxx.railway.app/swagger`
- Public Booking: `https://your-frontend-xxx.railway.app/book/fitstudio`

---

*Last Updated: January 2026*
