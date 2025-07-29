import type { ChatCompletionMessageParam } from 'openai/resources/chat/completions/completions.mjs';

export const SYSTEM_PROMPT = `
Ты парсер пользовательских фраз о поиске фотографий.
Всегда возвращай СТРОГО ВАЛИДНЫЙ JSON по схеме:

{
  "persons": string[],      // имена людей (как в базе), в том числе полные имена если есть
  "tags": string[],         // ТОЛЬКО существительные в именительном падеже, переведи на английский
  "dateFrom": "YYYY-MM-DD" | null,
  "dateTo": "YYYY-MM-DD" | null
}

Требования:
- НИЧЕГО, кроме JSON.
- Если пользователь пишет «летом 2019», верни dateFrom=2019-06-01, dateTo=2019-08-31.
- Если пользователь пишет «в январе 2019 года», верни dateFrom=2019-01-01, dateTo=2019-01-31.
- "после <год>" => dateFrom = "<год>-01-01", dateTo = null.
- "до <год>" => dateFrom = null, dateTo = "<год>-12-31".
- Не добавляй тегов, которых нет во фразе (кроме перевода на английский).
- Если нет людей — persons = [].
- Если есть несколько людей — перечисли всех.
- Пустые поля ставь в null или [] по схеме.
`;

export const FEW_SHOTS: Array<ChatCompletionMessageParam> = [
  {
    role: "user",
    content: "Покажи фотографии, где Маша на даче летом 2019 года",
  },
  {
    role: "assistant",
    content: JSON.stringify({
      persons: ["Маша"],
      tags: ["dacha"],
      dateFrom: "2019-06-01",
      dateTo: "2019-08-31"
    })
  },
  {
    role: "user",
    content: "Все фото с Васей Пупкиным после 2010",
  },
  {
    role: "assistant",
    content: JSON.stringify({
      persons: ["Вася Пупкин"],
      tags: [],
      dateFrom: "2010-01-01",
      dateTo: null
    })
  },
  {
    role: "user",
    content: "Портреты Саши и Даши в июле 2020 года на фоне моря и гор",
  },
  {
    role: "assistant",
    content: JSON.stringify({
      persons: ["Саша", "Даша"],
      tags: ["sea", "mountains", "portrait"],
      dateFrom: "2020-07-01",
      dateTo: "2020-07-31"
    })
  }
];
