/**
 * 通用组件共同的使用的基础组件，原先放在 adapter/form 内部，限制了使用范围，这里提取出来，方便其他地方使用
 * 可用于 vben-form、vben-modal、vben-drawer 等组件使用,
 */

import type { Component } from 'vue';

import type { BaseFormComponentType } from '@vben/common-ui';

import { defineAsyncComponent, h } from 'vue';

import { globalShareState } from '@vben/common-ui';

const Button = defineAsyncComponent(() => import('ant-design-vue/es/button'));
const DatePicker = defineAsyncComponent(
  () => import('ant-design-vue/es/date-picker'),
);
const RangePicker = defineAsyncComponent(() =>
  import('ant-design-vue/es/date-picker').then((res) => res.RangePicker),
);
const Input = defineAsyncComponent(() => import('ant-design-vue/es/input'));
const InputNumber = defineAsyncComponent(
  () => import('ant-design-vue/es/input-number'),
);
const InputPassword = defineAsyncComponent(() =>
  import('ant-design-vue/es/input').then((res) => res.InputPassword),
);
const Select = defineAsyncComponent(() => import('ant-design-vue/es/select'));
const Switch = defineAsyncComponent(() => import('ant-design-vue/es/switch'));
const Textarea = defineAsyncComponent(() =>
  import('ant-design-vue/es/input').then((res) => res.Textarea),
);
const TimePicker = defineAsyncComponent(
  () => import('ant-design-vue/es/time-picker'),
);

// 这里需要自行根据业务组件库进行适配，需要用到的组件都需要在这里类型说明
export type ComponentType =
  | 'DatePicker'
  | 'DefaultButton'
  | 'Input'
  | 'InputNumber'
  | 'InputPassword'
  | 'PrimaryButton'
  | 'RangePicker'
  | 'Select'
  | 'Switch'
  | 'Textarea'
  | 'TimePicker'
  | BaseFormComponentType;

async function initComponentAdapter() {
  const components: Partial<Record<ComponentType, Component>> = {
    DatePicker,
    // 自定义默认按钮
    DefaultButton: (props, { attrs, slots }) => {
      return h(Button, { ...props, attrs, type: 'default' }, slots);
    },
    Input,
    InputNumber,
    InputPassword,
    // 自定义主要按钮
    PrimaryButton: (props, { attrs, slots }) => {
      return h(Button, { ...props, attrs, type: 'primary' }, slots);
    },
    RangePicker,
    Select,
    Switch,
    Textarea,
    TimePicker,
  };

  // 将组件注册到全局共享状态中
  globalShareState.setComponents(components);

  // 定义全局共享状态中的消息提示
  globalShareState.defineMessage({
    // 复制成功消息提示
    copyPreferencesSuccess: (title, content) => {
      import('ant-design-vue').then(({ notification }) => {
        notification.success({
          description: content,
          message: title,
          placement: 'bottomRight',
        });
      });
    },
  });
}

export { initComponentAdapter };
