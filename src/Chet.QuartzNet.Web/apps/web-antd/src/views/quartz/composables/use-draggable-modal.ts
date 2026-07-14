import { watch, nextTick, type Ref } from 'vue';

/**
 * 让 ant-design-vue Modal 支持鼠标拖动
 * @param visible Modal 的 open 绑定 ref
 * @param wrapClassName Modal 的 wrapClassName（需唯一）
 */
export function useDraggableModal(visible: Ref<boolean>, wrapClassName: string) {
  let isDragging = false;
  let startX = 0;
  let startY = 0;
  let translateX = 0;
  let translateY = 0;

  const onMove = (e: MouseEvent) => {
    if (!isDragging) return;
    translateX = e.clientX - startX;
    translateY = e.clientY - startY;
    const modal = document.querySelector(
      `.${wrapClassName} .ant-modal`,
    ) as HTMLElement | null;
    if (modal) {
      modal.style.transform = `translate(${translateX}px, ${translateY}px)`;
    }
  };

  const onUp = () => {
    isDragging = false;
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onUp);
  };

  const onDown = (e: MouseEvent) => {
    // 排除关闭按钮区域
    if ((e.target as HTMLElement).closest('.ant-modal-close')) return;
    isDragging = true;
    startX = e.clientX - translateX;
    startY = e.clientY - translateY;
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  };

  const cleanup = () => {
    const header = document.querySelector(
      `.${wrapClassName} .ant-modal-header`,
    );
    header?.removeEventListener('mousedown', onDown as EventListener);
    const modal = document.querySelector(
      `.${wrapClassName} .ant-modal`,
    ) as HTMLElement | null;
    if (modal) modal.style.transform = '';
    translateX = 0;
    translateY = 0;
  };

  watch(visible, async (val) => {
    if (val) {
      await nextTick();
      requestAnimationFrame(() => {
        const header = document.querySelector(
          `.${wrapClassName} .ant-modal-header`,
        ) as HTMLElement | null;
        if (header) {
          header.style.cursor = 'move';
          header.style.userSelect = 'none';
          header.addEventListener('mousedown', onDown as EventListener);
        }
      });
    } else {
      cleanup();
    }
  });
}
