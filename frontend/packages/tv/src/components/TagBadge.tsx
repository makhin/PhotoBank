// src/components/TagBadge.tsx
import React from 'react';
import { View, Text } from 'react-native';
import { useThemedStyles } from '@/hooks/useThemedStyles';

export type BadgeVariant = 'tag' | 'person';

interface TagBadgeProps {
  label: string;
  variant?: BadgeVariant;
}

export const TagBadge: React.FC<TagBadgeProps> = ({ label, variant = 'tag' }) => {
  const { styles: appStyles } = useThemedStyles();

  const badgeStyle = variant === 'person' ? appStyles.badges.person : appStyles.badges.tag;
  const textStyle =
    variant === 'person'
      ? [appStyles.text.badge, appStyles.text.badgePerson]
      : [appStyles.text.badge, appStyles.text.badgeTag];

  return (
    <View style={[appStyles.badges.base, badgeStyle]}>
      <Text style={textStyle}>{label}</Text>
    </View>
  );
};

