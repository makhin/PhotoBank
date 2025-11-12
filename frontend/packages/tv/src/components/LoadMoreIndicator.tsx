// src/components/LoadMoreIndicator.tsx
import React from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import { useThemedStyles } from '@/hooks/useThemedStyles';

interface LoadMoreIndicatorProps {
  isVisible: boolean;
  loadedCount: number;
  totalCount: number;
  hasMore: boolean;
}

export const LoadMoreIndicator: React.FC<LoadMoreIndicatorProps> = ({
  isVisible,
  loadedCount,
  totalCount,
  hasMore,
}) => {
  const { styles: appStyles, colors } = useThemedStyles();

  if (!isVisible) return null;

  return (
    <View style={appStyles.feedback.block}>
      {hasMore ? (
        <>
          <ActivityIndicator size="large" color={colors.accentPrimary} />
          <Text style={appStyles.feedback.loaderText}>{`Загружено ${loadedCount} из ${totalCount}`}</Text>
        </>
      ) : (
        <>
          <Text style={appStyles.feedback.completeIcon}>🎉</Text>
          <Text style={appStyles.feedback.loaderText}>{`Все фото загружены (${loadedCount})`}</Text>
        </>
      )}
    </View>
  );
};

