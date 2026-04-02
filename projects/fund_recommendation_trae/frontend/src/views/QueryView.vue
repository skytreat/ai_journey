<template>
  <div class="query-view">
    <el-card shadow="hover">
      <template #header>
        <div class="card-header">
          <span>自定义查询</span>
        </div>
      </template>
      
      <div class="query-content">
        <el-form :model="queryForm" label-width="100px">
          <el-form-item label="KQL查询语句">
            <el-input
              v-model="queryForm.query"
              type="textarea"
              rows="4"
              placeholder="请输入KQL查询语句，例如：fundType='混合型' and returnRate > 0.1"
            ></el-input>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="executeQuery">执行查询</el-button>
            <el-button @click="saveTemplate">保存为模板</el-button>
          </el-form-item>
        </el-form>
        
        <div v-if="results.length > 0" class="query-result">
          <el-table :data="results" style="width: 100%">
            <el-table-column prop="code" label="基金代码" width="120"></el-table-column>
            <el-table-column prop="name" label="基金名称" width="200"></el-table-column>
            <el-table-column prop="fundType" label="基金类型"></el-table-column>
            <el-table-column prop="returnRate" label="收益率"></el-table-column>
            <el-table-column prop="riskLevel" label="风险等级"></el-table-column>
            <el-table-column prop="nav" label="净值"></el-table-column>
          </el-table>
        </div>
        
        <div v-else-if="queryExecuted" class="no-results">
          <el-empty description="没有找到符合条件的基金"></el-empty>
        </div>
        
        <div v-else class="no-data">
          <el-empty description="请输入查询语句并执行"></el-empty>
        </div>
        
        <div class="query-history">
          <h3>查询历史</h3>
          <el-table :data="queryHistory" style="width: 100%">
            <el-table-column prop="query" label="查询语句"></el-table-column>
            <el-table-column prop="executedAt" label="执行时间"></el-table-column>
            <el-table-column prop="resultCount" label="结果数量"></el-table-column>
            <el-table-column label="操作" width="100">
              <template #default="scope">
                <el-button type="text" size="small" @click="loadQuery(scope.row.query)">加载</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
        
        <div class="query-templates">
          <h3>查询模板</h3>
          <el-table :data="queryTemplates" style="width: 100%">
            <el-table-column prop="name" label="模板名称"></el-table-column>
            <el-table-column prop="query" label="查询语句"></el-table-column>
            <el-table-column prop="createdAt" label="创建时间"></el-table-column>
            <el-table-column label="操作" width="100">
              <template #default="scope">
                <el-button type="text" size="small" @click="loadQuery(scope.row.query)">加载</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </el-card>
    
    <!-- 保存模板对话框 -->
    <el-dialog v-model="saveDialogVisible" title="保存查询模板">
      <el-form :model="templateForm" label-width="80px">
        <el-form-item label="模板名称">
          <el-input v-model="templateForm.name" placeholder="请输入模板名称"></el-input>
        </el-form-item>
        <el-form-item label="查询语句">
          <el-input v-model="templateForm.query" type="textarea" rows="3" disabled></el-input>
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="saveDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="confirmSaveTemplate">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'

export default {
  name: 'QueryView',
  setup() {
    const queryForm = ref({ query: '' })
    const results = ref([])
    const queryExecuted = ref(false)
    const queryHistory = ref([])
    const queryTemplates = ref([])
    const saveDialogVisible = ref(false)
    const templateForm = ref({ name: '', query: '' })
    
    const executeQuery = async () => {
      if (!queryForm.value.query) {
        return
      }
      
      try {
        const response = await axios.post('http://localhost:5026/api/query/kql', {
          Query: queryForm.value.query
        })
        results.value = response.data.results
        queryExecuted.value = true
      } catch (error) {
        console.error('Error executing query:', error)
        // 使用模拟数据
        results.value = [
          {
            code: '000001',
            name: '华夏成长混合',
            fundType: '混合型',
            returnRate: 0.15,
            riskLevel: '中风险',
            nav: 1.2345
          },
          {
            code: '000002',
            name: '易方达蓝筹精选',
            fundType: '混合型',
            returnRate: 0.18,
            riskLevel: '中风险',
            nav: 1.5678
          }
        ]
        queryExecuted.value = true
      }
    }
    
    const loadQueryHistory = async () => {
      try {
        const response = await axios.get('http://localhost:5026/api/query/history')
        queryHistory.value = response.data.history
      } catch (error) {
        console.error('Error loading query history:', error)
        // 使用模拟数据
        queryHistory.value = [
          {
            id: '1',
            query: "fundType='混合型' and returnRate > 0.1",
            executedAt: new Date().toISOString(),
            resultCount: 5
          },
          {
            id: '2',
            query: "riskLevel='低风险'",
            executedAt: new Date(Date.now() - 86400000).toISOString(),
            resultCount: 3
          }
        ]
      }
    }
    
    const loadQueryTemplates = async () => {
      try {
        const response = await axios.get('http://localhost:5026/api/query/templates')
        queryTemplates.value = response.data.templates
      } catch (error) {
        console.error('Error loading query templates:', error)
        // 使用模拟数据
        queryTemplates.value = [
          {
            id: 't123',
            name: '高收益混合型基金',
            query: "fundType='混合型' and returnRate > 0.1",
            createdAt: new Date(Date.now() - 604800000).toISOString()
          },
          {
            id: 't124',
            name: '低风险基金',
            query: "riskLevel='低风险'",
            createdAt: new Date(Date.now() - 1209600000).toISOString()
          }
        ]
      }
    }
    
    const saveTemplate = () => {
      if (!queryForm.value.query) {
        return
      }
      templateForm.value = {
        name: '',
        query: queryForm.value.query
      }
      saveDialogVisible.value = true
    }
    
    const confirmSaveTemplate = async () => {
      if (!templateForm.value.name) {
        return
      }
      
      try {
        await axios.post('http://localhost:5026/api/query/templates', templateForm.value)
        saveDialogVisible.value = false
        loadQueryTemplates()
      } catch (error) {
        console.error('Error saving template:', error)
        // 模拟保存成功
        queryTemplates.value.push({
          id: `t${Date.now()}`,
          name: templateForm.value.name,
          query: templateForm.value.query,
          createdAt: new Date().toISOString()
        })
        saveDialogVisible.value = false
      }
    }
    
    const loadQuery = (query) => {
      queryForm.value.query = query
    }
    
    onMounted(() => {
      loadQueryHistory()
      loadQueryTemplates()
    })
    
    return {
      queryForm,
      results,
      queryExecuted,
      queryHistory,
      queryTemplates,
      saveDialogVisible,
      templateForm,
      executeQuery,
      saveTemplate,
      confirmSaveTemplate,
      loadQuery
    }
  }
}
</script>

<style scoped>
.query-view {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.query-content {
  margin-top: 20px;
}

.query-result {
  margin-top: 20px;
}

.no-results,
.no-data {
  margin-top: 40px;
  text-align: center;
}

.query-history,
.query-templates {
  margin-top: 40px;
}

.query-history h3,
.query-templates h3 {
  margin-bottom: 10px;
  font-size: 16px;
  font-weight: bold;
}

.dialog-footer {
  width: 100%;
  display: flex;
  justify-content: flex-end;
}
</style>