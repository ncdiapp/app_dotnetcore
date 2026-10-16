# PLM — Google Cloud Service Account Setup Guide (Production)

This guide walks through the complete setup of a Google Cloud service account for PLM AI image features in a production environment. Follow every step in order.

**Time required:** approximately 30 minutes  
**Who performs this:** the IT administrator responsible for the PLM server and the Google Cloud account for your organization

---

## What this enables

Completing this guide activates the following PLM features:

| Feature | Description |
| --- | --- |
| Image Analysis | Gemini analyses product images and returns descriptions, attributes, or comparisons |
| Image Generation | Gemini generates or edits product images from a text prompt |
| Find Similar Products | Finds visually similar products using AI image embeddings |
| Image Search *(optional)* | "Search Image on Google" using Vertex AI Search |

---

## Before you start

You need:

- A **Google account with Owner or Editor access** to a Google Cloud project, or the ability to create one
- **Administrator access to the PLM server** (Windows / IIS) to copy a file and restart the app pool
- **SQL Server access** to the PLM database to run a short UPDATE script

If you do not already have a Google Cloud project for PLM, create one at [console.cloud.google.com](https://console.cloud.google.com) before continuing.

---

## Step 1 — Note your Google Cloud project ID

1. Open [console.cloud.google.com](https://console.cloud.google.com).
2. In the top toolbar, click the project selector (next to the Google Cloud logo).
3. Find or create the project you will use for PLM.
4. Note the **Project ID** — it looks like `my-company-plm-12345`. You will use it in later steps.

> The Project ID is different from the Project Name. Use the ID (lowercase letters, digits, hyphens).

---

## Step 2 — Enable the required APIs

1. In the Google Cloud Console, make sure your PLM project is selected.
2. Open the left menu → **APIs & Services → Library**.
3. Search for and enable each of the following:

| API | Search term | Required for |
| --- | --- | --- |
| **Agent Platform API** | `Agent Platform` | All AI image features |
| **Discovery Engine API** | `Discovery Engine` | Image Search only — skip if not using Image Search |

> In the Google Cloud Console this API appears as **Agent Platform API** (not "Vertex AI API"). Search for `Agent Platform` to find it.

To enable an API: click its name in the search results → click **Enable**. Wait for the page to confirm it is enabled before continuing.

---

## Step 3 — Create a service account

A service account is the identity PLM uses to call Google APIs. It is separate from any personal Google account.

1. Open the left menu → **IAM & Admin → Service Accounts**.
2. Click **+ Create Service Account**.
3. Fill in the form:

   | Field | Value |
   | --- | --- |
   | Service account name | `plm-ai-service` |
   | Service account ID | `plm-ai-service` *(auto-filled)* |
   | Description | `PLM AI image and Gemini service account` |

4. Click **Create and Continue**.
5. On the **Grant access** step — do not add any roles here. Click **Continue**.
6. On the **Grant users access** step — leave blank. Click **Done**.

The service account is now created. Its email address will look like:

```
plm-ai-service@YOUR-PROJECT-ID.iam.gserviceaccount.com
```

Keep this page open — you will need the email in the next step.

---

## Step 4 — Grant the service account the required roles

1. Open the left menu → **IAM & Admin → IAM**.
2. Click **+ Grant Access**.
3. In the **New principals** field, paste the service account email from Step 3.
4. Under **Assign roles**, add the following:

   | PLM feature | Role name | Role ID |
   | --- | --- | --- |
   | All AI image features (Agent Platform API) | **Vertex AI User** | `roles/aiplatform.user` |
   | Image Search *(optional)* | **Discovery Engine Viewer** | `roles/discoveryengine.viewer` |

   To find a role: click **Select a role** → type the role name in the search box → select it.  
   To add a second role: click **+ Add another role**.

5. Click **Save**.

> If you are not using the Image Search feature, only the **Vertex AI User** role is needed.

---

## Step 5 — Create and download a JSON key

The JSON key file allows PLM to authenticate as the service account. Treat it like a password.

1. Open the left menu → **IAM & Admin → Service Accounts**.
2. Click the service account you created in Step 3 (`plm-ai-service@…`).
3. Open the **Keys** tab.
4. Click **Add Key → Create new key**.
5. Select **JSON** → click **Create**.

The browser downloads a `.json` file automatically. This file is downloaded **only once**. Store it immediately in a secure location.

> **Security rules for the key file:**
> - Do not email it, paste it into a chat, or commit it to Git
> - Do not put it inside the PLM web application folder (`wwwroot`, `inetpub`, or the app directory)
> - Do not share it with anyone who does not need to configure the PLM server

---

## Step 6 — Deploy the key file on the PLM server

Perform the following steps on the **Windows server running PLM**.

### 6a. Copy the key file to a protected location

Create a dedicated secrets folder outside the web application directory:

```
C:\secrets\
```

Copy the downloaded JSON file into that folder and rename it for clarity:

```
C:\secrets\plm-ai-service-key.json
```

### 6b. Restrict file permissions

The key file must be readable by the IIS application pool identity but not by general users.

1. Right-click `plm-ai-service-key.json` → **Properties → Security tab**.
2. Click **Advanced → Disable Inheritance → Convert**.
3. Remove all users except:
   - The IIS application pool identity (e.g. `IIS AppPool\PLM` or `NETWORK SERVICE`) — grant **Read** only
   - The local Administrator account — grant **Full control**
4. Click **OK** and **Apply**.

To find the correct application pool identity: open **IIS Manager → Application Pools**, locate the pool used by PLM, and note the identity shown in the **Identity** column.

---

## Step 7 — Configure PLM database settings

Connect to the PLM SQL Server database and run the following script. Replace the values marked with `← CHANGE THIS` with your actual values.

```sql
-- Switch PLM to service account authentication (mode 2)
UPDATE pdmSetup
SET SetupValue = '2'
WHERE setupCode = 'AiGeminiApiMode';

-- Your Google Cloud project ID  ← CHANGE THIS
UPDATE pdmSetup
SET SetupValue = 'your-project-id'
WHERE setupCode = 'AiGoogleProjectId';

-- Vertex AI region (us-central1 is the default; change only if your project uses a different region)
UPDATE pdmSetup
SET SetupValue = 'us-central1'
WHERE setupCode = 'AiGoogleLocation';

-- Full path to the JSON key file on the PLM server
UPDATE pdmSetup
SET SetupValue = 'C:\secrets\plm-ai-service-key.json'
WHERE setupCode = 'AiGoogleServiceAccountPath';

-- Gemini model for chat and image analysis (default — change only if advised by PLM support)
UPDATE pdmSetup
SET SetupValue = 'gemini-2.5-flash'
WHERE setupCode = 'AiGeminiDefaultModel';

-- Gemini model for image generation and editing (default — change only if advised by PLM support)
UPDATE pdmSetup
SET SetupValue = 'gemini-2.5-flash-image'
WHERE setupCode = 'AiGeminiImageModel';
```

After running the script, verify the rows were updated:

```sql
SELECT setupCode, SetupValue
FROM pdmSetup
WHERE setupCode IN (
    'AiGeminiApiMode',
    'AiGoogleProjectId',
    'AiGoogleLocation',
    'AiGoogleServiceAccountPath',
    'AiGeminiDefaultModel',
    'AiGeminiImageModel'
);
```

---

## Step 8 — Restart the PLM application

Changes to the database settings take effect after the PLM application pool is recycled.

1. Open **IIS Manager** on the PLM server.
2. Expand the server node → click **Application Pools**.
3. Right-click the PLM application pool → **Recycle**.

Alternatively, restart the entire IIS service:

```powershell
iisreset
```

---

## Step 9 — Verify the setup

Log in to PLM and test each feature you enabled:

| Test | How to verify |
| --- | --- |
| Image Analysis | Open a product, click the AI image analysis button, upload or select an image. Gemini should return a description. |
| Image Generation | Open the AI image generation panel, enter a prompt, and click Generate. An image should appear within a few seconds. |
| Find Similar Products | Open a product with an image, click Find Similar. A grid of visually similar products should appear. |
| Image Search *(if configured)* | Use the Search Image on Google feature. Results should appear from the Vertex AI Search index. |

**If a feature fails**, check the PLM error log and look for messages containing any of the following:

| Error message | Likely cause |
| --- | --- |
| `AiGeminiApiMode = 2` required | `AiGeminiApiMode` is not set to `2` in the database |
| `AiGoogleProjectId is not configured` | `AiGoogleProjectId` row is missing or empty |
| `Could not load file` / `Access denied` | The IIS app pool identity cannot read the JSON key file |
| `403 Permission denied` / `PERMISSION_DENIED` | The service account is missing `roles/aiplatform.user` |
| `Vertex AI API has not been used` | The Agent Platform API was not enabled in the Google Cloud project |

---

## Step 10 — Optional: enable Image Search (Discovery Engine)

Skip this step if PLM's "Search Image on Google" feature is not required.

This feature uses Google Cloud **AI Applications** (Discovery Engine), which lets PLM search for similar product images across a web data store.

### 10a. Open AI Applications

1. Open [console.cloud.google.com](https://console.cloud.google.com) and select your PLM project.
2. In the left menu, click **AI Applications** (or navigate directly to `console.cloud.google.com/gen-app-builder/engines`).
3. You will see the **Apps** page. The current location defaults to **global** — leave it as global.

### 10b. Create a data store

A data store is the indexed content that the search app queries.

1. In the left menu under AI Applications, click **Data Stores**.
2. Click **+ Create data store**.
3. Select **Web content** as the source type.
4. Enter the URLs of your product image website (e.g. your public product catalogue). Google will crawl and index these pages.
5. Give the data store a name (e.g. `plm-fashion-sites`) and click **Create**.

Wait for the initial indexing to complete before creating the app. This can take several hours for large sites.

### 10c. Create a Search app

1. On the **Apps** page, click **+ Create app**.
2. Select **Search** as the app type.
3. Fill in the form:

   | Field | Value |
   | --- | --- |
   | App name | `PLM Web Image Search` *(or any name)* |
   | Location | `global` |
   | Connected data stores | Select the data store created in Step 10b |

4. Click **Create**.

### 10d. Copy the Engine ID

After the app is created, the **Apps** list shows an **ID** column. Copy the full ID — it looks like:

```
plm-dev-web-image-search-xxxxx_1
```

You will need the complete ID string exactly as shown.

### 10e. Save the Engine ID in PLM

```sql
UPDATE pdmSetup
SET SetupValue = 'your-full-engine-id-here'
WHERE setupCode = 'AiGoogleVertexAiSearchEngineId';
```

### 10f. Confirm the IAM role

Ensure the service account has `roles/discoveryengine.viewer` (granted in Step 4). Without this role, search requests return a 403 permission error.

Recycle the IIS application pool after saving the Engine ID.

---

## Ongoing maintenance

### Annual key rotation

Service account JSON keys should be rotated at least once per year and immediately if the file may have been exposed.

1. In the Google Cloud Console → **IAM & Admin → Service Accounts → Keys tab**, click **Add Key → Create new key → JSON**.
2. Copy the new JSON file to `C:\secrets\plm-ai-service-key.json` on the PLM server, replacing the old file.
3. Verify the file permissions match those set in Step 6b.
4. Recycle the IIS application pool.
5. Test image features to confirm the new key works.
6. Delete the old key from the **Keys** tab in the Google Cloud Console.

### Revoking an exposed key

If the key file was accidentally shared, emailed, or committed to a repository:

1. Go to **IAM & Admin → Service Accounts → Keys tab**.
2. Find the affected key by its ID (shown in the JSON file as `"private_key_id"`).
3. Click the delete icon to revoke it immediately.
4. Create a new key and follow the rotation steps above before restoring service.

---

## Reference: PLM database settings

| `setupCode` | Description | Example value |
| --- | --- | --- |
| `AiGeminiApiMode` | `1` = API key (default), `2` = service account | `2` |
| `AiGoogleProjectId` | Google Cloud project ID | `my-company-plm-12345` |
| `AiGoogleLocation` | Vertex AI region | `us-central1` |
| `AiGoogleServiceAccountPath` | Full path to the JSON key file on the server | `C:\secrets\plm-ai-service-key.json` |
| `AiGeminiDefaultModel` | Gemini model for chat and image analysis | `gemini-2.5-flash` |
| `AiGeminiImageModel` | Gemini model for image generation and editing | `gemini-2.5-flash-image` |
| `AiGoogleVertexAiSearchEngineId` | Vertex AI Search engine ID *(Image Search only)* | *(provided by Google after app creation)* |

---

## Support

If you encounter errors not covered by the verification table in Step 9, contact PLM support and provide:

- The exact error message from the PLM error log
- The output of the verification query from Step 7
- The Google Cloud project ID and region
- Whether the Vertex AI API shows as **Enabled** in the Google Cloud Console
