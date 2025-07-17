import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

export const PhotoFilters = () => (
    <View style={styles.container}>
      <Text style={styles.title}>Фильтры (Теги, Даты)</Text>
      {/* Кнопки фильтров будут здесь */}
    </View>
);

const styles = StyleSheet.create({
  container: { padding: 80 },
  title: { color: '#fff', fontSize: 96, marginBottom: 40 },
});
