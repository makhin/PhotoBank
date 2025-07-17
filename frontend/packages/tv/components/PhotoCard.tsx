import React from 'react';
import { TouchableOpacity, Image, StyleSheet, Text, View } from 'react-native';
import { PhotoItemDto } from '@photobank/shared/types';

export const PhotoCard = ({ photo, onPress }: { photo: PhotoItemDto; onPress: () => void }) => (
    <TouchableOpacity style={styles.card} onPress={onPress}>
      <Image
          source={{ uri: `data:image/jpeg;base64,${photo.thumbnail}` }}
          style={styles.image}
          resizeMode="cover"
      />
      <View style={styles.infoContainer}>
          <Text style={styles.name}>{photo.name}</Text>
          {photo.takenDate && <Text style={styles.date}>{photo.takenDate}</Text>}
          {photo.captions && photo.captions.length > 0 && (
              <Text style={styles.caption}>{photo.captions[0]}</Text>
          )}
      </View>
    </TouchableOpacity>
);

const styles = StyleSheet.create({
  card: {
    width: 50,
    height: 50,
    marginHorizontal: 20,
    borderRadius: 10,
    overflow: 'hidden',
    backgroundColor: '#000',
  },
  image: { width: '100%', height: '100%' },
  infoContainer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    width: '100%',
    backgroundColor: 'rgba(0,0,0,0.7)',
    padding: 2,
  },
  name: { color: '#fff', fontSize: 8 },
  date: { color: '#ccc', fontSize: 6 },
  caption: { color: '#aaa', fontSize: 6 },
});
