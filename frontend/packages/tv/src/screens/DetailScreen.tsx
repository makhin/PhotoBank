// src/screens/DetailScreen.tsx
import React, { useEffect, useState } from 'react';
import {
  View,
  Image,
  ActivityIndicator,
  Text,
  Platform,
  Pressable,
  TVEventHandler as TVEventHandlerClass,
} from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '@/navigation/RootNavigator';
import { usePhotosGetPhoto } from '@/api/generated/photos/photos';
import { useAppStore } from '@/store/useAppStore';
import { useThemedStyles } from '@/hooks/useThemedStyles';
import { resolveMediaUrl } from '@/utils/urlHelpers';

type Props = NativeStackScreenProps<RootStackParamList, 'Detail'>;

const DetailScreen: React.FC<Props> = ({ route, navigation }) => {
  const { photoId, photoIds } = route.params;
  const { setFocusedItemId } = useAppStore();
  const { styles: appStyles, colors } = useThemedStyles();
  const [imageLoading, setImageLoading] = useState(true);
  const [imageError, setImageError] = useState(false);

  const { data: photo, isLoading, isError } = usePhotosGetPhoto(photoId);

  // Находим текущий индекс фото в списке
  const currentIndex = photoIds.indexOf(photoId);

  // Reset image states when photoId changes (MUST be before conditional returns)
  useEffect(() => {
    setImageLoading(true);
    setImageError(false);
  }, [photoId]);

  // Сохраняем текущий photoId при каждом изменении
  useEffect(() => {
    console.log('Current photoId changed to:', photoId);
    setFocusedItemId(String(photoId));
  }, [photoId, setFocusedItemId]);

  // Используем TVEventHandler через addListener
  useEffect(() => {
    if (!Platform.isTV) return;

    console.log('Setting up TV event listener...');

    const handleTVEvent = (evt: any) => {
      console.log('TV Event received:', evt.eventType);

      if (evt.eventType === 'up') {
        // Предыдущее фото
        if (currentIndex > 0) {
          const previousPhotoId = photoIds[currentIndex - 1];
          console.log('Going to previous photo:', previousPhotoId);
          navigation.setParams({ photoId: previousPhotoId });
        }
      } else if (evt.eventType === 'down') {
        // Следующее фото
        if (currentIndex < photoIds.length - 1) {
          const nextPhotoId = photoIds[currentIndex + 1];
          console.log('Going to next photo:', nextPhotoId);
          navigation.setParams({ photoId: nextPhotoId });
        }
      } else if (evt.eventType === 'right') {
        // Переход на экран метаданных
        console.log('Navigating to metadata screen');
        navigation.navigate('Metadata', { photoId, photoIds });
      } else if (evt.eventType === 'left') {
        // Возврат на HomeScreen (photoId уже сохранен в useEffect)
        console.log('Going back to home screen with photoId:', photoId);
        navigation.goBack();
      }
    };

    const subscription = (TVEventHandlerClass as any).addListener(handleTVEvent);
    console.log('TV event listener added');

    return () => {
      if (subscription && subscription.remove) {
        subscription.remove();
        console.log('TV event listener removed');
      }
    };
  }, [currentIndex, photoIds, photoId, navigation]);

  if (isLoading) {
    return (
      <View style={appStyles.layout.centeredOverlay}>
        <ActivityIndicator size="large" color={colors.accentPrimary} />
        <Text style={appStyles.text.status}>Загрузка фото...</Text>
      </View>
    );
  }

  if (isError || !photo) {
    return (
      <View style={[appStyles.layout.centeredOverlay, appStyles.insets.sectionPadding]}>
        <Text style={appStyles.text.error}>Ошибка загрузки фото</Text>
      </View>
    );
  }

  return (
    <Pressable
      style={appStyles.layout.centeredOverlay}
      onPress={() => console.log('Screen pressed')}
      hasTVPreferredFocus={true}
    >
      {photo.previewUrl && !imageError && (
        <Image
          source={{ uri: resolveMediaUrl(photo.previewUrl) }}
          style={appStyles.media.fullscreenImage}
          resizeMode="contain"
          onLoadStart={() => setImageLoading(true)}
          onLoadEnd={() => setImageLoading(false)}
          onError={() => {
            console.error('Failed to load preview image:', resolveMediaUrl(photo.previewUrl));
            setImageLoading(false);
            setImageError(true);
          }}
        />
      )}

      {/* Show loading indicator while image is loading */}
      {imageLoading && !imageError && photo.previewUrl && (
        <View style={appStyles.layout.centeredOverlay}>
          <ActivityIndicator size="large" color={colors.accentPrimary} />
        </View>
      )}

      {/* Show error message if image failed to load */}
      {imageError && (
        <View style={[appStyles.layout.centeredOverlay, appStyles.insets.sectionPadding]}>
          <Text style={appStyles.text.error}>Не удалось загрузить изображение</Text>
          <Text style={appStyles.text.status}>{resolveMediaUrl(photo.previewUrl)}</Text>
        </View>
      )}

      {/* Photo counter */}
      <View style={appStyles.media.counter}>
        <Text style={appStyles.media.counterText}>
          {currentIndex + 1} / {photoIds.length}
        </Text>
      </View>
    </Pressable>
  );
};

export default DetailScreen;
