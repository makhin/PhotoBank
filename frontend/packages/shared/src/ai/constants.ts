import type { ChatCompletionMessageParam } from 'openai/resources';

export const SYSTEM_PROMPT = `
You are a parser of user phrases for photo searches.
Always return STRICTLY VALID JSON according to the schema:

{
  "personNames": string[],  // names of people, including full names if present
  "tagNames": string[],     // ONLY nouns in the nominative case
  "dateFrom": "YYYY-MM-DD" | null,
  "dateTo": "YYYY-MM-DD" | null
}

Requirements:
- NOTHING except JSON.
- If the user writes "in summer 2019", return dateFrom=2019-06-01, dateTo=2019-08-31.
- If the user writes "in January 2019", return dateFrom=2019-01-01, dateTo=2019-01-31.
- "after <year>" => dateFrom = "<year>-01-01", dateTo = null.
- "before <year>" => dateFrom = null, dateTo = "<year>-12-31".
- Do not add tags that are not present in the phrase.
- If there are no people — personNames = [].
- If there are several people — list them all.
- Empty fields must be set to null or [] according to the schema.
`;

export const FEW_SHOTS: Array<ChatCompletionMessageParam> = [
  {
    role: "user",
    content: "Покажи фотографии, где Маша на даче летом 2019 года",
  },
  {
    role: "assistant",
    content: JSON.stringify({
      personNames: ["Маша"],
      tagNames: ["дача"],
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
      personNames: ["Вася Пупкин"],
      tagNames: [],
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
      personNames: ["Саша", "Даша"],
      tagNames: ["море", "горы", "портрет"],
      dateFrom: "2020-07-01",
      dateTo: "2020-07-31"
    })
  }
];
