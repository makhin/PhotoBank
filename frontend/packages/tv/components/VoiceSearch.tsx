import React, { useRef, useState } from 'react';
import { View, TextInput, StyleSheet, TouchableOpacity } from 'react-native';
import { MaterialIcons } from '@expo/vector-icons';

interface VoiceSearchProps {
  onSearch: (query: string) => void;
}

export const VoiceSearch = ({ onSearch }: VoiceSearchProps) => {
  const [visible, setVisible] = useState(false);
  const [query, setQuery] = useState('');
  const recognitionRef = useRef<SpeechRecognition | null>(null);

  const startRecognition = () => {
    const SpeechRecognitionImpl =
      (window as Window & {
        webkitSpeechRecognition?: typeof SpeechRecognition;
      }).SpeechRecognition ||
      (window as Window & {
        webkitSpeechRecognition?: typeof SpeechRecognition;
      }).webkitSpeechRecognition;
    if (!SpeechRecognitionImpl) {
      return;
    }
    const recognition: SpeechRecognition = new SpeechRecognitionImpl();
    recognition.lang = 'ru-RU';
    recognition.onresult = (event: SpeechRecognitionEvent) => {
      const text = event.results[0][0].transcript;
      setQuery(text);
      onSearch(text);
    };
    recognition.onend = () => {
      recognitionRef.current = null;
    };
    recognitionRef.current = recognition;
    recognition.start();
  };

  const handlePress = () => {
    if (!visible) {
      setVisible(true);
      startRecognition();
    } else {
      if (recognitionRef.current) {
        recognitionRef.current.stop();
      }
      setVisible(false);
      setQuery('');
      onSearch('');
    }
  };

  return (
    <View style={styles.container}>
      {visible && (
        <TextInput
          style={styles.input}
          value={query}
          onChangeText={(text) => {
            setQuery(text);
            onSearch(text);
          }}
          placeholder="Скажите что-нибудь..."
        />
      )}
      <TouchableOpacity onPress={handlePress} style={styles.micButton}>
        <MaterialIcons name="mic" size={24} color="#fff" />
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
  },
  input: {
    flex: 1,
    backgroundColor: '#fff',
    color: '#000',
    height: 40,
    borderRadius: 4,
    paddingHorizontal: 8,
    marginRight: 8,
  },
  micButton: {
    padding: 8,
    backgroundColor: '#1f2937',
    borderRadius: 20,
  },
});

export default VoiceSearch;
