import React from 'react';
import { View, Text, FlatList, StyleSheet } from 'react-native';
import { PhotoItemDto } from '@photobank/shared/api/photobank';

import { PhotoCard } from './PhotoCard';

export const PhotoRow = ({ title, photos }: { title: string; photos: PhotoItemDto[] }) => (
    <View style={styles.row}>
        <Text style={styles.title}>{title}</Text>
        <FlatList
            horizontal
            data={photos}
            renderItem={({ item }) => <PhotoCard photo={item} />}
            keyExtractor={(item) => item.id.toString()}
        />
    </View>
);

const styles = StyleSheet.create({
    row: { marginBottom: 80 },
    title: { color: '#fff', fontSize: 72, marginLeft: 40, marginBottom: 20 },
});
