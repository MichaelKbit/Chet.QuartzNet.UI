<template>
  <div class="log-management">
    <div class="log-search">
      <Form
        :model="searchForm"
        layout="inline"
        @finish="handleSearch"
        class="search-form"
      >
        <FormItem label="作业名称" name="jobName">
          <Input v-model:value="searchForm.jobName" placeholder="请输入作业名称" allow-clear />
        </FormItem>
        <FormItem label="执行状态" name="status">
          <Select v-model:value="searchForm.status" placeholder="请选择执行状态" allow-clear>
            <SelectOption value="SUCCESS">成功</SelectOption>
            <SelectOption value="FAILURE">失败</SelectOption>
          </Select>
        </FormItem>
        <FormItem label="执行时间" name="executionTime">
          <DatePicker.RangePicker
            v-model:value="searchForm.executionTime"
            show-time
            style="width: 100%"
          />
        </FormItem>
        <FormItem>
          <Button type="primary" html-type="submit">搜索</Button>
        </FormItem>
        <FormItem>
          <Button @click="handleReset">重置</Button>
        </FormItem>
      </Form>
    </div>
    <div class="log-table">
      <Button danger type="primary" @click="handleClearLogs" class="clear-btn" :disabled="loading">
        清空日志
      </Button>
      <Table
        :columns="logColumns"
        :data-source="logsData"
        :row-key="(record) => record.id"
        :pagination="logPagination"
        @change="handleTableChange"
        size="middle"
        :loading="loading"
      >
        <template #status="{ text }">
          <Tag :color="getStatusColor(text)">{{ getStatusText(text) }}</Tag>
        </template>
        <template #errorMsg="{ text }">
          <Tooltip :title="text || '无错误信息'" placement="top">
            <span>{{ text ? '查看错误详情' : '无错误' }}</span>
          </Tooltip>
        </template>
      </Table>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue';
import { message } from 'ant-design-vue';
import { Table } from 'ant-design-vue';
import { Form, FormItem } from 'ant-design-vue';
import { Input } from 'ant-design-vue';
import { Button } from 'ant-design-vue';
import { Select, SelectOption } from 'ant-design-vue';
import { DatePicker } from 'ant-design-vue';
import { Tag } from 'ant-design-vue';
import { Tooltip } from 'ant-design-vue';
// 暂时使用原生Date格式化替代

// 搜索表单
    const searchForm = reactive({
      jobName: '',
      status: '',
      executionTime: [],
    });

    // 分页配置
    const logPagination = reactive({
      current: 1,
      pageSize: 10,
      total: 0,
    });

    // 日志数据
    const logsData = ref([]);

    // 加载状态
    const loading = ref(false);

    // 日志列配置
    const logColumns = [
      {
        title: '日志ID',
        dataIndex: 'id',
        key: 'id',
        ellipsis: true,
        width: 100
      },
      {
        title: '作业名称',
        dataIndex: 'jobName',
        key: 'jobName',
        ellipsis: true,
        width: 180
      },
      {
        title: '作业组',
        dataIndex: 'jobGroup',
        key: 'jobGroup',
        ellipsis: true,
        width: 150
      },
      {
        title: '开始时间',
        dataIndex: 'startTime',
        key: 'startTime',
        ellipsis: true,
        width: 180,
        customRender: ({ text }) => {
            if (!text) return '';
            const date = new Date(text);
            return date.toISOString().replace('T', ' ').substring(0, 19);
          }
      },
      {
        title: '结束时间',
        dataIndex: 'endTime',
        key: 'endTime',
        ellipsis: true,
        width: 180,
        customRender: ({ text }) => {
            if (!text) return '';
            const date = new Date(text);
            return date.toISOString().replace('T', ' ').substring(0, 19);
          }
      },
      {
        title: '状态',
        dataIndex: 'status',
        key: 'status',
        slots: {
          customRender: 'status'
        }
      },
      {
        title: '错误信息',
        dataIndex: 'errorMsg',
        key: 'errorMsg',
        ellipsis: true,
        width: 300,
        slots: {
          customRender: 'errorMsg'
        }
      },
    ];

    // 获取状态文本
    const getStatusText = (status: string): string => {
      const statusMap: Record<string, string> = {
        'SUCCESS': '成功',
        'FAILURE': '失败',
      };
      return statusMap[status] || '未知';
    };

    // 获取状态颜色
    const getStatusColor = (status: string): string => {
      const colorMap: Record<string, string> = {
        'SUCCESS': 'success',
        'FAILURE': 'error',
      };
      return colorMap[status] || 'default';
    };

    // 格式化日期时间
    const formatDateTime = (dateString: string): string => {
      if (!dateString) return '';
      try {
        const date = new Date(dateString);
        return date.toISOString().replace('T', ' ').substring(0, 19);
      } catch (error) {
        return dateString;
      }
    };

    // 获取日志列表
    const fetchLogs = async () => {
      try {
        loading.value = true;
        // 这里应该是实际的API调用
        // 模拟API调用
        // const response = await api.getLogs({
        //   page: logPagination.current,
        //   pageSize: logPagination.pageSize,
        //   jobName: searchForm.jobName,
        //   status: searchForm.status,
        //   startTime: searchForm.executionTime[0],
        //   endTime: searchForm.executionTime[1],
        // });
        // logsData.value = response.data;
        // logPagination.total = response.total;
        
        // 模拟数据
        logsData.value = [
          {
            id: '1',
            jobName: 'TestJob1',
            jobGroup: 'DEFAULT',
            startTime: formatDateTime(new Date().toISOString()),
            endTime: formatDateTime(new Date().toISOString()),
            status: 'SUCCESS',
          },
          {
            id: '2',
            jobName: 'TestJob2',
            jobGroup: 'DEFAULT',
            startTime: formatDateTime(new Date().toISOString()),
            endTime: formatDateTime(new Date().toISOString()),
            status: 'FAILURE',
            errorMsg: '执行失败：无法连接到数据库',
          },
        ];
        logPagination.total = 100;
      } catch (error) {
        console.error('获取日志失败:', error);
        message.error('获取日志失败');
      } finally {
        loading.value = false;
      }
    };

    // 搜索日志
    const handleSearch = () => {
      logPagination.current = 1;
      fetchLogs();
    };

    // 重置搜索条件
    const handleReset = () => {
      searchForm.jobName = '';
      searchForm.status = '';
      searchForm.executionTime = [];
      logPagination.current = 1;
      fetchLogs();
    };

    // 处理表格分页变化
    const handleTableChange = (pagination: any) => {
      logPagination.current = pagination.current;
      logPagination.pageSize = pagination.pageSize;
      fetchLogs();
    };

    // 清空日志
    const handleClearLogs = () => {
      // 这里应该是实际的API调用
      // api.clearLogs().then(() => {
      //   message.success('日志清空成功');
      //   fetchLogs();
      // }).catch(() => {
      //   message.error('日志清空失败');
      // });
      message.success('日志清空成功');
      fetchLogs();
    };

    // 组件挂载时获取日志列表
    onMounted(() => {
      fetchLogs();
    });

    return {
      searchForm,
      logPagination,
      logsData,
      loading,
      logColumns,
      getStatusText,
      getStatusColor,
      handleSearch,
      handleReset,
      handleTableChange,
      handleClearLogs,
    };
  },
</script>

<style lang="less" scoped>
.log-management {
  padding: 20px;
  .log-search {
    margin-bottom: 20px;
    .search-form {
      background: #fff;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }
  }
  .log-table {
    background: #fff;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    .clear-btn {
      margin-bottom: 16px;
    }
  }
  

}

@media (max-width: 768px) {
  .log-management {
    padding: 10px;
    .log-search,
    .log-table {
      padding: 15px;
    }
  }
}
</style>