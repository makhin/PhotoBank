import React from 'react';
import { View, Image, Text, StyleSheet, TouchableOpacity, ActivityIndicator } from 'react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { usePhotoById } from '../hooks/usePhotoApi';

// Определяем список маршрутов и их параметры
type RootStackParamList = {
    PhotoViewer: {
        photoId: number;
        photoIds: number[];
    };
    // если есть другие экраны, добавь их здесь
};

type PhotoViewerRouteProp = RouteProp<RootStackParamList, 'PhotoViewer'>;
type PhotoViewerNavigationProp = NativeStackNavigationProp<RootStackParamList, 'PhotoViewer'>;

export const PhotoViewer = () => {
    const route = useRoute<PhotoViewerRouteProp>();
    const navigation = useNavigation<PhotoViewerNavigationProp>();
    const { photoId, photoIds } = route.params;

    const currentIndex = photoIds.indexOf(photoId);
    const prevId = currentIndex > 0 ? photoIds[currentIndex - 1] : null;
    const nextId = currentIndex < photoIds.length - 1 ? photoIds[currentIndex + 1] : null;

    const { photo, loading } = usePhotoById(photoId);

    return (
        <View style={styles.container}>
            {loading ? (
                <ActivityIndicator size="large" color="#fff" />
            ) : photo ? (
                <>
                    <Image
                        source={{ uri: `data:image/jpeg;base64,${photo.previewImage}` }}
                        style={styles.image}
                        resizeMode="contain"
                    />
                    <View style={styles.infoContainer}>
                        <Text style={styles.title}>{photo.name}</Text>
                        <Text style={styles.subtitle}>{photo.takenDate}</Text>
                    </View>
                    {prevId && (
                        <TouchableOpacity
                            style={styles.leftNav}
                            onPress={() =>
                                navigation.navigate('PhotoViewer', { photoId: prevId, photoIds })
                            }
                        >
                            <Text style={styles.navText}>⟵</Text>
                        </TouchableOpacity>
                    )}
                    {nextId && (
                        <TouchableOpacity
                            style={styles.rightNav}
                            onPress={() =>
                                navigation.navigate('PhotoViewer', { photoId: nextId, photoIds })
                            }
                        >
                            <Text style={styles.navText}>⟶</Text>
                        </TouchableOpacity>
                    )}
                    <TouchableOpacity
                        style={styles.closeButton}
                        onPress={() => navigation.goBack()}
                    >
                        <Text style={styles.closeText}>✕</Text>
                    </TouchableOpacity>
                </>
            ) : (
                <Text style={styles.error}>Ошибка загрузки фото</Text>
            )}
        </View>
    );
};

const styles = StyleSheet.create({
    container: { flex: 1, backgroundColor: '#000', width: 3840, height: 2160 },
    image: { width: '100%', height: '100%' },
    infoContainer: {
        position: 'absolute',
        bottom: 0,
        width: '100%',
        backgroundColor: 'rgba(0,0,0,0.8)',
        padding: 80,
    },
    title: { color: '#fff', fontSize: 96, fontWeight: 'bold' },
    subtitle: { color: '#ccc', fontSize: 64, marginTop: 20 },
    leftNav: { position: 'absolute', left: 0, top: '50%', padding: 40 },
    rightNav: { position: 'absolute', right: 0, top: '50%', padding: 40 },
    navText: { color: '#fff', fontSize: 160 },
    closeButton: { position: 'absolute', top: 40, right: 40, padding: 40 },
    closeText: { color: '#fff', fontSize: 80 },
    error: { color: '#fff', textAlign: 'center', marginTop: 200, fontSize: 96 },
});
