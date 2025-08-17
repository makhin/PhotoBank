import React, { useState } from 'react';
import { ScrollView, StyleSheet } from 'react-native';

import { PhotoRow } from '../components/PhotoRow';
import { usePhotos } from '../hooks/usePhotoApi';
import { VoiceSearch } from '../components/VoiceSearch';

export const HomeScreen = () => {
    const [caption, setCaption] = useState('');
    const { photos } = usePhotos(caption);

    return (
        <ScrollView style={styles.container}>
            <VoiceSearch onSearch={setCaption} />
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
