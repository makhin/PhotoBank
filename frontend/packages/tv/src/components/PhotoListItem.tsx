// src/components/PhotoListItem.tsx
import React, { useState } from 'react';
import { Pressable, Text, Image, View } from 'react-native';
import { format } from 'date-fns';
import { TagBadge } from './TagBadge';
import { useThemedStyles } from '@/hooks/useThemedStyles';
import { resolveMediaUrl } from '@/utils/urlHelpers';

interface PhotoListItemProps {
  thumbnailUrl?: string | null;
  name: string;
  storageName: string;
  takenDate?: Date | null;
  captions?: string[] | null;
  personNames?: string[]; // Преобразованные имена персон
  tagNames?: string[]; // Преобразованные имена тегов
  isAdultContent?: boolean;
  onPress: () => void;
  onFocus?: () => void;
  hasTVPreferredFocus?: boolean;
}

export const PhotoListItem: React.FC<PhotoListItemProps> = ({
  thumbnailUrl,
  name,
  storageName,
  takenDate,
  captions,
  personNames = [],
  tagNames = [],
  isAdultContent = false,
  onPress,
  onFocus,
  hasTVPreferredFocus = false,
}) => {
  const { styles: appStyles } = useThemedStyles();
  const [isFocused, setIsFocused] = useState(false);
  const [imageError, setImageError] = useState(false);

  const handleFocus = () => {
    setIsFocused(true);
    onFocus?.();
  };

  const handleBlur = () => {
    setIsFocused(false);
  };

  // Форматирование даты в dd.MM.yyyy HH:mm
  const formatDate = (date?: Date | null): string => {
    if (!date) return '';
    try {
      return format(date, 'dd.MM.yyyy HH:mm');
    } catch {
      return '';
    }
  };

  const formattedDate = formatDate(takenDate);
  const caption = captions && captions.length > 0 ? captions[0] : '';

  // Ограничиваем количество показываемых бейджей
  const MAX_BADGES = 5;
  const visiblePersons = personNames.slice(0, MAX_BADGES);
  const remainingPersons = personNames.length - MAX_BADGES;
  const visibleTags = tagNames.slice(0, MAX_BADGES);
  const remainingTags = tagNames.length - MAX_BADGES;

  return (
    <Pressable
      style={[appStyles.list.itemContainer, isFocused && appStyles.list.itemFocused]}
      onPress={onPress}
      onFocus={handleFocus}
      onBlur={handleBlur}
      hasTVPreferredFocus={hasTVPreferredFocus}
    >
      {/* Thumbnail */}
      <View style={appStyles.list.thumbnailContainer}>
        {thumbnailUrl && !imageError ? (
          <Image
            source={{ uri: resolveMediaUrl(thumbnailUrl) }}
            style={appStyles.list.thumbnail}
            resizeMode="cover"
            onError={() => {
              console.warn('Failed to load thumbnail:', resolveMediaUrl(thumbnailUrl));
              setImageError(true);
            }}
          />
        ) : (
          <View style={[appStyles.list.thumbnail, appStyles.list.thumbnailPlaceholder]}>
            <Text style={appStyles.list.placeholderText}>?</Text>
          </View>
        )}
        {/* NSFW Badge */}
        {isAdultContent && (
          <View style={appStyles.list.nsfwBadge}>
            <Text style={appStyles.list.nsfwText}>18+</Text>
          </View>
        )}
      </View>

      {/* Content */}
      <View style={[appStyles.list.content, appStyles.list.contentGap]}>
        {/* Row 1: Storage, Name, Date, Caption */}
        <View style={appStyles.list.row}>
          <Text style={[appStyles.list.rowText, isFocused && appStyles.list.rowTextFocused]} numberOfLines={1}>
            📁 {storageName}
          </Text>
          <Text style={appStyles.list.separator}>•</Text>
          <Text
            style={[
              appStyles.list.rowText,
              appStyles.list.nameText,
              isFocused && appStyles.list.rowTextFocused,
            ]}
            numberOfLines={1}
          >
            {name}
          </Text>
          {formattedDate && (
            <>
              <Text style={appStyles.list.separator}>•</Text>
              <Text style={[appStyles.list.rowText, isFocused && appStyles.list.rowTextFocused]}>
                🕒 {formattedDate}
              </Text>
            </>
          )}
          {caption && (
            <>
              <Text style={appStyles.list.separator}>•</Text>
              <Text
                style={[appStyles.list.rowText, isFocused && appStyles.list.rowTextFocused]}
                numberOfLines={1}
              >
                💬 {caption}
              </Text>
            </>
          )}
        </View>

        {/* Row 2: Persons, Tags */}
        <View style={appStyles.list.row}>
          {/* Persons */}
          {personNames.length > 0 && (
            <View style={appStyles.list.badgesContainer}>
              {visiblePersons.map((person, index) => (
                <TagBadge key={`person-${index}`} label={person} variant="person" />
              ))}
              {remainingPersons > 0 && <TagBadge label={`+${remainingPersons}`} variant="person" />}
            </View>
          )}

          {/* Tags */}
          {tagNames.length > 0 && (
            <View style={appStyles.list.badgesContainer}>
              {visibleTags.map((tag, index) => (
                <TagBadge key={`tag-${index}`} label={tag} variant="tag" />
              ))}
              {remainingTags > 0 && <TagBadge label={`+${remainingTags}`} variant="tag" />}
            </View>
          )}
        </View>
      </View>
    </Pressable>
  );
};

