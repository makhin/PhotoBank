import React from 'react';
import { ScrollView, StyleSheet } from 'react-native';

import { PhotoRow } from '../components/PhotoRow';
import { usePhotos } from '../hooks/usePhotoApi';

export const HomeScreen = () => {
    const { photos } = usePhotos();

    return (
        <ScrollView style={styles.container}>
            <PhotoRow title="Photos" photos={photos} />
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
