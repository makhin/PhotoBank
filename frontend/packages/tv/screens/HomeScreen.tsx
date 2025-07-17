import React from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { PhotoRow } from '../components/PhotoRow';
import { usePhotos } from '../hooks/usePhotoApi';
import {DEFAULT_PHOTO_FILTER} from "@photobank/shared/constants";

export const HomeScreen = () => {
    const { photos, loading } = usePhotos(DEFAULT_PHOTO_FILTER);

    return (
        <ScrollView style={styles.container}>
            {photos.map((row, idx) => (
                <PhotoRow key={idx} title={row.name} photos={photos} />
            ))}
        </ScrollView>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#000',
        width: 3840,
        height: 2160,
    },
});
