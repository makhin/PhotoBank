/* eslint-disable @typescript-eslint/no-explicit-any */
import React from 'react';

export const View = (props: any) => React.createElement('View', props, props.children);
export const Image = (props: any) => React.createElement('Image', props, props.children);
export const Text = (props: any) => React.createElement('Text', props, props.children);
export const StyleSheet = { create: (styles: any) => styles };
