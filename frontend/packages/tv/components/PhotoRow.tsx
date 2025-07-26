import React from 'react';
import { View, Text, FlatList, StyleSheet } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { PhotoItemDto } from '@photobank/shared/generated';

import { PhotoCard } from './PhotoCard';

export const PhotoRow = ({ title, photos }: { title: string; photos: PhotoItemDto[] }) => {
    const navigation = useNavigation();

    return (
        <View style={styles.row}>
            <Text style={styles.title}>{title}</Text>
            <FlatList
                horizontal
                data={photos}
                renderItem={({ item }) => (
                    <PhotoCard
                        photo={item}
                        onPress={() =>
                            { navigation.navigate('PhotoViewer', {
                                photoId: item.id,
                                photoIds: photos.map((p) => p.id),
                            }); }
                        }
                    />
                )}
                keyExtractor={(item) => item.id.toString()}
            />
        </View>
    );
};

const styles = StyleSheet.create({
    row: { marginBottom: 80 },
    title: { color: '#fff', fontSize: 72, marginLeft: 40, marginBottom: 20 },
});
