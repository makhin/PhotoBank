import React from 'react';
import { TouchableOpacity, Image, StyleSheet } from 'react-native';
import { PhotoItemDto } from '@photobank/shared/types';

export const PhotoCard = ({ photo, onPress }: { photo: PhotoItemDto; onPress: () => void }) => (
    <TouchableOpacity style={styles.card} onPress={onPress}>
      <Image
          source={{ uri: `data:image/jpeg;base64,${photo.thumbnail}` }}
          style={styles.image}
          resizeMode="cover"
      />
    </TouchableOpacity>
);

const styles = StyleSheet.create({
  card: {
    width: 640,
    height: 360,
    marginHorizontal: 20,
    borderRadius: 20,
    overflow: 'hidden',
  },
  image: { width: '100%', height: '100%' },
});
