import { describe, expect, it } from 'vitest';

import { i18n } from '../src/i18n';

describe('search usage localization', () => {
  it('returns the full tip list in Russian', () => {
    expect(i18n.t('ru', 'search-usage')).toBe(
      [
        '❗ Советы по /search:',
        '1. Введите текст подписи или фразу в кавычках — ищем по caption.',
        '2. Добавляйте теги: #семья или tags:family,kids.',
        '3. Уточняйте людей: @anna или people:anna,ivan.',
        '4. Фильтруйте по датам: date:2020, date:2020-07..2020-08, одиночные 2020-05-15 или границы before:2020-01 / after:2019.',
      ].join('\n'),
    );
  });

  it('returns the full tip list in English', () => {
    expect(i18n.t('en', 'search-usage')).toBe(
      [
        '❗ Tips for /search:',
        '1. Type caption keywords or wrap phrases in quotes — searches by caption.',
        '2. Add tags: #family or tags:family,kids.',
        '3. Narrow by people: @anna or people:anna,ivan.',
        '4. Filter by dates: date:2020, date:2020-07..2020-08, single 2020-05-15, or bounds like before:2020-01 / after:2019.',
      ].join('\n'),
    );
  });
});
