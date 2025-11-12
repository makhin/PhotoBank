// src/screens/MetadataScreen.tsx
import React, { useEffect } from 'react';
import {
  View,
  ActivityIndicator,
  Text,
  Platform,
  TVEventHandler as TVEventHandlerClass,
} from 'react-native';
import { CommonActions } from '@react-navigation/native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '@/navigation/RootNavigator';
import { usePhotosGetPhoto } from '@/api/generated/photos/photos';
import { format } from 'date-fns';
import { TagBadge } from '@/components/TagBadge';
import { useThemedStyles } from '@/hooks/useThemedStyles';

type Props = NativeStackScreenProps<RootStackParamList, 'Metadata'>;

const MetadataScreen: React.FC<Props> = ({ route, navigation }) => {
  const { photoId, photoIds } = route.params;
  const { styles: appStyles, colors } = useThemedStyles();

  const { data: photo, isLoading, isError } = usePhotosGetPhoto(photoId);

  // Обработка событий пульта TV - только LEFT для возврата
  useEffect(() => {
    if (!Platform.isTV) return;

    console.log('MetadataScreen: Setting up TV event listener...');

    const handleTVEvent = (evt: any) => {
      console.log('MetadataScreen TV Event received:', evt.eventType);

      if (evt.eventType === 'left') {
        console.log('Returning to detail screen');

        navigation.dispatch((state) => {
          const targetRoute = state.routes.find((route) => route.name === 'Detail');

          if (!targetRoute) {
            return null;
          }

          return CommonActions.setParams({
            params: { photoId, photoIds },
            source: targetRoute.key,
          });
        });

        if (navigation.canGoBack()) {
          navigation.goBack();
        }
      }
    };

    const subscription = (TVEventHandlerClass as any).addListener(handleTVEvent);
    console.log('MetadataScreen: TV event listener added');

    return () => {
      if (subscription && subscription.remove) {
        subscription.remove();
        console.log('MetadataScreen: TV event listener removed');
      }
    };
  }, [navigation, photoId, photoIds]);

  // Находим текущий индекс фото в списке
  const currentIndex = photoIds.indexOf(photoId);

  if (isLoading) {
    return (
      <View style={appStyles.layout.centeredScreen}>
        <ActivityIndicator size="large" color={colors.accentPrimary} />
        <Text style={appStyles.text.status}>Загрузка метаданных...</Text>
      </View>
    );
  }

  if (isError || !photo) {
    return (
      <View style={[appStyles.layout.centeredScreen, appStyles.insets.sectionPadding]}>
        <Text style={appStyles.text.error}>Ошибка загрузки метаданных</Text>
      </View>
    );
  }

  return (
    <View style={[appStyles.layout.screen, appStyles.insets.screenPadding]}>
      <View style={[appStyles.layout.row, appStyles.gaps.lg]}>
        {/* Left column */}
        <View style={appStyles.layout.columnFlex}>
          {/* Basic info */}
          <View style={appStyles.sections.container}>
            <Text style={appStyles.text.headingSection}>Основная информация</Text>

            {photo.name && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Файл:</Text>
                <Text style={appStyles.info.value} numberOfLines={1}>
                  {photo.name}
                </Text>
              </View>
            )}

            {photo.takenDate && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Дата съёмки:</Text>
                <Text style={appStyles.info.value}>
                  {format(photo.takenDate, 'dd.MM.yyyy HH:mm:ss')}
                </Text>
              </View>
            )}

            {photo.id !== undefined && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>ID:</Text>
                <Text style={appStyles.info.value}>{photo.id}</Text>
              </View>
            )}

            {photo.width !== undefined && photo.height !== undefined && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Разрешение:</Text>
                <Text style={appStyles.info.value}>
                  {photo.width} × {photo.height}
                </Text>
              </View>
            )}

            {photo.scale !== undefined && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Масштаб:</Text>
                <Text style={appStyles.info.value}>{photo.scale.toFixed(2)}</Text>
              </View>
            )}

            {photo.orientation !== undefined && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Ориентация:</Text>
                <Text style={appStyles.info.value}>{photo.orientation}</Text>
              </View>
            )}

            {photo.location && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Координаты:</Text>
                <Text style={appStyles.info.value}>
                  {photo.location.latitude?.toFixed(6)}, {photo.location.longitude?.toFixed(6)}
                </Text>
              </View>
            )}
          </View>

          {/* Captions */}
          {photo.captions && photo.captions.length > 0 && (
            <View style={appStyles.sections.container}>
              <Text style={appStyles.text.headingSection}>Описания ({photo.captions.length})</Text>
              {photo.captions.slice(0, 5).map((caption, index) => (
                <View key={index} style={appStyles.info.captionItem}>
                  <Text style={appStyles.info.captionText} numberOfLines={2}>
                    {caption}
                  </Text>
                </View>
              ))}
              {photo.captions.length > 5 && (
                <Text style={appStyles.info.moreText}>+{photo.captions.length - 5} ещё</Text>
              )}
            </View>
          )}

          {/* Faces */}
          {photo.faces && photo.faces.length > 0 && (
            <View style={appStyles.sections.container}>
              <Text style={appStyles.text.headingSection}>
                Распознанные лица ({photo.faces.length})
              </Text>
              <View style={appStyles.info.badgesContainer}>
                {photo.faces.slice(0, 10).map((face, index) => (
                  <View key={index} style={appStyles.info.faceItem}>
                    <Text style={appStyles.info.faceText}>
                      {face.age ? `Возраст: ${face.age}` : `Лицо ${index + 1}`}
                    </Text>
                  </View>
                ))}
                {photo.faces.length > 10 && (
                  <Text style={appStyles.info.moreText}>+{photo.faces.length - 10} ещё</Text>
                )}
              </View>
            </View>
          )}
        </View>

        {/* Right column */}
        <View style={appStyles.layout.columnFlex}>
          {/* Tags */}
          {photo.tags && photo.tags.length > 0 && (
            <View style={appStyles.sections.container}>
              <Text style={appStyles.text.headingSection}>Теги ({photo.tags.length})</Text>
              <View style={appStyles.info.badgesContainer}>
                {photo.tags.slice(0, 20).map((tag, index) => (
                  <TagBadge key={`tag-${index}`} label={tag} variant="tag" />
                ))}
                {photo.tags.length > 20 && (
                  <TagBadge label={`+${photo.tags.length - 20}`} variant="tag" />
                )}
              </View>
            </View>
          )}

          {/* Content moderation */}
          <View style={appStyles.sections.container}>
            <Text style={appStyles.text.headingSection}>Модерация контента</Text>

            {photo.adultScore !== undefined && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Adult Score:</Text>
                <View style={appStyles.scores.container}>
                  <Text
                    style={[
                      appStyles.scores.value,
                      photo.adultScore > 0.7
                        ? appStyles.scores.high
                        : photo.adultScore > 0.4
                        ? appStyles.scores.medium
                        : appStyles.scores.low,
                    ]}
                  >
                    {(photo.adultScore * 100).toFixed(1)}%
                  </Text>
                  {photo.adultScore > 0.7 && <Text style={appStyles.scores.icon}>🔞</Text>}
                </View>
              </View>
            )}

            {photo.racyScore !== undefined && (
              <View style={appStyles.sections.row}>
                <Text style={appStyles.info.label}>Racy Score:</Text>
                <View style={appStyles.scores.container}>
                  <Text
                    style={[
                      appStyles.scores.value,
                      photo.racyScore > 0.7
                        ? appStyles.scores.high
                        : photo.racyScore > 0.4
                        ? appStyles.scores.medium
                        : appStyles.scores.low,
                    ]}
                  >
                    {(photo.racyScore * 100).toFixed(1)}%
                  </Text>
                  {photo.racyScore > 0.7 && <Text style={appStyles.scores.icon}>⚠️</Text>}
                </View>
              </View>
            )}

            {photo.adultScore === undefined && photo.racyScore === undefined && (
              <Text style={appStyles.info.noFlags}>✓ Нет данных модерации</Text>
            )}
          </View>
        </View>
      </View>
    </View>
  );
};

export default MetadataScreen;
