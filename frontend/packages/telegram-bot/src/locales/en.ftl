help = Available commands:\n/thisday [page] – show photos of this day\n/search <caption> – search by caption\n/ai <prompt> – AI search\n/photo <id> – show photo by ID\n/profile – profile info\n/subscribe HH:MM – daily /thisday digest\n/tags [prefix] – list tags\n/storages [prefix] – list storages and paths\n/persons [prefix] – list persons\n\nAny message without command is treated as an /ai request.
cmd-start = Start the bot
cmd-help = Help
cmd-thisday = Today's photos
cmd-search = Search by caption
cmd-ai = AI search
cmd-profile = Profile info
cmd-subscribe = Subscribe to daily /thisday
cmd-tags = List tags
cmd-persons = List persons
cmd-storages = List storages
cmd-upload = Upload files
welcome = Welcome. Bot started!
caption-missing = No caption.
photo-usage = ❗ Use: /photo <id>
photo-not-found = ❌ Photo not found.
subscribe-usage = ❗ Use: /subscribe HH:MM
search-usage = ❗ Советы по /search:
1. Введите текст подписи или фразу в кавычках — ищем по caption.
2. Добавляйте теги: #семья или tags:family,kids.
3. Уточняйте людей: @anna или people:anna,ivan.
4. Фильтруйте по датам: date:2020, date:2020-07..2020-08, одиночные 2020-05-15 или границы before:2020-01 / after:2019.

Tips for /search:
1. Type caption keywords or wrap phrases in quotes — searches by caption.
2. Add tags: #family or tags:family,kids.
3. Narrow by people: @anna or people:anna,ivan.
4. Filter by dates: date:2020, date:2020-07..2020-08, single 2020-05-15, or bounds like before:2020-01 / after:2019.
ai-usage = ❗ Use: /ai <prompt>
ai-filter-empty = ⚠️ Could not determine filter from request. Please clarify.
todays-photos-empty = 📭 No photos for today yet.
search-photos-empty = 📭 No photos found for your query.
not-registered = ⚠️ Your Telegram is not registered. Contact administrator. ID: { $userId }
upload-success = ✅ Files uploaded.
upload-failed = 🚫 Failed to upload files.
unknown-year = Unknown year
unknown-person = Unknown
first-page = ⏮ First
prev-page = ◀ Back
next-page = Next ▶
last-page = Last ⏭
roles-label = Roles:
roles-empty = No roles.
get-profile-error = 🚫 Failed to get user profile.
page-info = 📄 Page { $page } of { $total }
user-info = 👤 User: { $username }
chat-undetermined = ❌ Error: cannot determine chat.
subscription-confirmed = ✅ Subscribed to daily digest at { $time } UTC.
sorry-try-later = ⚠️ Sorry, try again later.
inline-link-account = Link your account to search photos
inline-search-failed = Search failed (retry?)
deeplink-not-linked = Your Telegram is not linked. Contact administrator to link.
deeplink-inline-example = Example inline query: @botname cats, @botname date:2024
start-linked = ✅ Your Telegram is linked.
tags-error = 🚫 Failed to fetch tags.
persons-error = 🚫 Failed to fetch persons.
storages-error = 🚫 Failed to fetch storages.
people-count = 👥 { $count } ppl.
untitled = Untitled
unknown-message = Received another message!
